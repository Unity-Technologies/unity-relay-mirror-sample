using Mirror;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Burst;
using UnityEngine;
using System.Collections.Generic;

namespace Utp
{
    [BurstCompile]
    struct ClientUpdateJob : IJob
    {
        /// <summary>
        /// Used to bind, listen, and send data to connections.
        /// </summary>
        public NetworkDriver driver;

        /// <summary>
        /// client connections to this server.
        /// </summary>
        public Unity.Networking.Transport.NetworkConnection connection;

        /// <summary>
        /// Temporary storage for connection events that occur on job threads so they may be dequeued on the main thread.
        /// </summary>
        public NativeQueue<UtpConnectionEvent>.ParallelWriter connectionEventsQueue;

        /// <summary>
        /// Process all incoming events/messages on this connection.
        /// </summary>
        public void Execute()
        {
            if (!connection.IsCreated)
            {
                return;
            }

            DataStreamReader stream;
            NetworkEvent.Type netEvent;
            while ((netEvent = connection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
            {
                if (netEvent == NetworkEvent.Type.Connect)
                {
                    UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
                    connectionEvent.eventType = (byte)UtpConnectionEventType.OnConnected;
                    connectionEvent.connectionId = connection.GetHashCode();

                    connectionEventsQueue.Enqueue(connectionEvent);
                }
                else if (netEvent == NetworkEvent.Type.Data)
                {
                    NativeArray<byte> nativeMessage = new NativeArray<byte>(stream.Length, Allocator.Temp);
                    stream.ReadBytes(nativeMessage);

                    UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
                    connectionEvent.eventType = (byte)UtpConnectionEventType.OnReceivedData;
                    connectionEvent.connectionId = connection.GetHashCode();
					connectionEvent.eventData = GetFixedList(nativeMessage);

					connectionEventsQueue.Enqueue(connectionEvent);
                }
                else if (netEvent == NetworkEvent.Type.Disconnect)
                {
                    UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
                    connectionEvent.eventType = (byte)UtpConnectionEventType.OnDisconnected;
                    connectionEvent.connectionId = connection.GetHashCode();

                    connectionEventsQueue.Enqueue(connectionEvent);
                }
            }
        }

        /// <summary>
        /// Translates a native array into a fixed list to send as event data.
        /// </summary>
        /// <param name="data">The message data in a native array.</param>
        /// <returns>The message data in a fixed list.</returns>
        public FixedList4096Bytes<byte> GetFixedList(NativeArray<byte> data)
        {
            FixedList4096Bytes<byte> retVal = new FixedList4096Bytes<byte>();
            unsafe
            {
                retVal.AddRange(NativeArrayUnsafeUtility.GetUnsafePtr(data), data.Length - 1);
            }
            return retVal;
        }
    }

    [BurstCompile]
    struct ClientSendJob : IJob
    {
        /// <summary>
        /// Used to bind, listen, and send data to connections.
        /// </summary>
        public NetworkDriver driver;

        /// <summary>
        /// client connections to this server.
        /// </summary>
        public Unity.Networking.Transport.NetworkConnection connection;

        /// <summary>
        /// The buffer to copy from.
        /// </summary>
        //public ArraySegment<byte> segment;
        public NativeSlice<byte> segment;

        /// <summary>
        /// The specific channel ID to operate on.
        /// </summary>
        public int channelId;

        /// <summary>
        /// The reliable network pipeline for ensured packet send order.
        /// </summary>
        public NetworkPipeline reliablePipeline;

        /// <summary>
        /// Unreliable pipeline for fast, unensured packet send order.
        /// </summary>
        public NetworkPipeline unreliablePipeline;

        public void Execute()
        {
            NetworkPipeline pipeline = channelId == Channels.Reliable ? reliablePipeline : unreliablePipeline;

            DataStreamWriter writer;
            int writeStatus = driver.BeginSend(pipeline, connection, out writer);
            if (writeStatus == 0)
            {
                NativeArray<byte> nativeMessage = new NativeArray<byte>(segment.Length, Allocator.Temp);
                segment.CopyTo(nativeMessage);
                writer.WriteBytes(nativeMessage);
                driver.EndSend(writer);
            }
        }
    }

    /// <summary>
    /// A client for Mirror using UTP.
    /// </summary>
    public class UtpClient
	{
		// Events
		public Action OnConnected;
		public Action<ArraySegment<byte>> OnReceivedData;
		public Action OnDisconnected;

        /// <summary>
        /// The UTP logger.
        /// </summary>
        public UtpLog logger;

        /// <summary>
        /// Temporary storage for connection events that occur on job threads so they may be dequeued on the main thread.
        /// </summary>
        private NativeQueue<UtpConnectionEvent> connectionEventsQueue;

        /// <summary>
        /// Used alongside a connection to connect, send, and receive data from a listen server.
        /// </summary>
        private NetworkDriver driver;

        /// <summary>
        /// Used alongside a driver to connect, send, and receive data from a listen server.
        /// </summary>
        private Unity.Networking.Transport.NetworkConnection connection;

		/// <summary>
		/// A pipeline on the driver that is sequenced, and ensures messages are delivered.
		/// </summary>
		private NetworkPipeline reliablePipeline;

		/// <summary>
		/// A pipeline on the driver that is sequenced, but does not ensure messages are delivered.
		/// </summary>
		private NetworkPipeline unreliablePipeline;

        /// <summary>
        /// Job handle to schedule client jobs.
        /// </summary>
		private JobHandle clientJobHandle;

        /// <summary>
        /// Timeout(ms) to be set on drivers.
        /// </summary>
        private int timeout;

		public UtpClient(Action OnConnected, Action<ArraySegment<byte>> OnReceivedData, Action OnDisconnected, int timeout)
		{
            logger = new UtpLog("[Client] ");
            this.OnConnected = OnConnected;
			this.OnReceivedData = OnReceivedData;
			this.OnDisconnected = OnDisconnected;
            this.timeout = timeout;
		}

		/// <summary>
		/// Attempt to connect to a listen server at a given IP/port. Currently only supports IPV4.
		/// </summary>
		/// <param name="host">The host address at which the listen server is running.</param>
		/// <param name="port">The port which the listen server is listening on.</param>
		public void Connect(string host, ushort port)
		{
            clientJobHandle.Complete();

			if (IsConnected())
			{
                logger.Warning("Client is already connected");
				return;
            }

            var settings = new NetworkSettings();
            settings.WithNetworkConfigParameters(disconnectTimeoutMS: timeout);

            driver = NetworkDriver.Create(settings);
            reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            unreliablePipeline = driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            connection = new Unity.Networking.Transport.NetworkConnection();
            connectionEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);

			if (host == "localhost")
			{
				host = "127.0.0.1";
			}

			NetworkEndPoint endpoint = NetworkEndPoint.Parse(host, port); // TODO: also support IPV6
			connection = driver.Connect(endpoint);

            logger.Info("Client connecting to server at " + endpoint.Address);
		}

        /// <summary>
        /// Attempt to connect to a Relay host given a join allocation.
        /// </summary>
        /// <param name="joinAllocation"></param>
		public void RelayConnect(JoinAllocation joinAllocation)
		{
			if (IsConnected())
			{
                logger.Warning("Client is already connected");
				return;
			}

			RelayServerData relayServerData = RelayUtils.PlayerRelayData(joinAllocation, "udp");
			RelayNetworkParameter relayNetworkParameter = new RelayNetworkParameter { ServerData = relayServerData };
			NetworkSettings networkSettings = new NetworkSettings();
            RelayParameterExtensions.WithRelayParameters(ref networkSettings, ref relayServerData);

            driver = NetworkDriver.Create(networkSettings);
			reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
			unreliablePipeline = driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
			connection = new Unity.Networking.Transport.NetworkConnection();
			connectionEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);

            connection = driver.Connect(relayNetworkParameter.ServerData.Endpoint);

            logger.Info("Client connecting to server at " + relayNetworkParameter.ServerData.Endpoint.Address);
		}

		/// <summary>
		/// Whether or not the client is connected to a server.
		/// </summary>
		/// <returns>True if connected to a server, false otherwise.</returns>
		public bool IsConnected()
		{
			return DriverActive() &&
				connection.GetState(driver) == Unity.Networking.Transport.NetworkConnection.State.Connected;
		}

		/// <summary>
		/// Whether or not the network driver has been initialized.
		/// </summary>
		/// <returns>True if initialized, false otherwise.</returns>
		private bool DriverActive()
		{
			return !Equals(driver, default(NetworkDriver));
		}

		/// <summary>
		/// Disconnect from a listen server.
		/// </summary>
		public void Disconnect()
		{
            clientJobHandle.Complete();

            if (connection.IsCreated)
			{
                logger.Info("Disconnecting from server");

				connection.Disconnect(driver);
				// When disconnecting, we need to ensure the driver has the opportunity to send a disconnect event to the server
				driver.ScheduleUpdate().Complete();

				OnDisconnected.Invoke();
            }

			if (connectionEventsQueue.IsCreated)
			{
				ProcessIncomingEvents(); // Ensure we flush the queue
				connectionEventsQueue.Dispose();
			}

			if (connection.IsCreated)
			{
                connection.Close(driver);
			}

			if (driver.IsCreated)
			{
				driver.Dispose();
				driver = default(NetworkDriver);
			}
		}

		/// <summary>
		/// Tick the client, creating the client job and scheduling it. Processes incoming events 
		/// </summary>
		public void Tick()
		{
            // First complete the job that was initialized in the previous frame
            clientJobHandle.Complete();

            // Trigger Mirror callbacks for events that resulted in the last jobs work
            ProcessIncomingEvents();

            // Need to ensure the driver did not become inactive
            if (!DriverActive())
                return;

            // Create a new job
            var job = new ClientUpdateJob
            {
                driver = driver,
                connection = connection,
				connectionEventsQueue = connectionEventsQueue.AsParallelWriter()
            };

            // Schedule job
            clientJobHandle = driver.ScheduleUpdate();
            clientJobHandle = job.Schedule(clientJobHandle);
        }

        /// <summary>
        /// Send data to the listen server over a particular channel.
        /// </summary>
        /// <param name="segment">The data to send.</param>
        /// <param name="channelId">The 'Mirror.Channels' channel to send the data over.</param>
        public void Send(ArraySegment<byte> segment, int channelId)
		{
            // First complete the job that was initialized in the previous frame
            clientJobHandle.Complete();

            // Trigger Mirror callbacks for events that resulted in the last jobs work
            ProcessIncomingEvents();

            // Need to ensure the driver did not become inactive
            if (!DriverActive())
                return;

            //Convert ArraySegment to non-managed NativeSlice
            NativeSlice<byte> segmentSlice = new NativeSlice<byte>(
                new NativeArray<byte>(
                    segment.Array, 
                    Allocator.Persistent
                )
            );

            // Create a new job
            var job = new ClientSendJob
            {
                driver = driver,
                connection = connection,
                segment = segmentSlice,
                channelId = channelId,
                reliablePipeline = reliablePipeline,
                unreliablePipeline = unreliablePipeline
            };

            // Schedule job
            clientJobHandle = driver.ScheduleUpdate();
            clientJobHandle = job.Schedule(clientJobHandle);
        }

        /// <summary>
        /// Processes connection events from the queue.
        /// </summary>
        public void ProcessIncomingEvents()
        {
            // Exit if the driver is not active
            if (!DriverActive() || !NetworkClient.active)
                return;

            // Exit if the connection is not ready
            if (!connection.IsCreated || !connection.IsCreated)
                return;

            UtpConnectionEvent connectionEvent;
            while (connectionEventsQueue.IsCreated && connectionEventsQueue.TryDequeue(out connectionEvent))
            {
				if (connectionEvent.eventType == (byte)UtpConnectionEventType.OnConnected)
                {
					OnConnected.Invoke();
                }
				else if (connectionEvent.eventType == (byte)UtpConnectionEventType.OnReceivedData)
                {
					OnReceivedData.Invoke(new ArraySegment<byte>(connectionEvent.eventData.ToArray()));
                }
				else if (connectionEvent.eventType == (byte)UtpConnectionEventType.OnDisconnected)
                {
					OnDisconnected.Invoke();
                }
                else
                {
                    logger.Warning("invalid connection event: " + connectionEvent.eventType);
                }
            }
        }

        /// <summary>
		/// Exposes driver's max header size for UTP Transport.
		/// </summary>
		/// <param name="channelId">The channel ID.</param>
		/// <returns>The max header size of the network driver.</returns>
		public int GetMaxHeaderSize(int channelId = Channels.Reliable)
        {
            return driver.MaxHeaderSize(reliablePipeline);
        }

        /// <summary>
		/// Enables logging for this module.
		/// </summary>
		/// <param name="logLevel">The log level to set this logger to.</param>
		public void EnableLogging(LogLevel logLevel = LogLevel.Verbose)
        {
            logger.SetLogLevel(logLevel);
        }

        /// <summary>
        /// Disables logging for this module.
        /// </summary>
        public void DisableLogging()
        {
            logger.SetLogLevel(LogLevel.Off);
        }
    }
}