using Mirror;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using System.Text;

namespace Utp
{
    #region Jobs

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

        public FixedList4096Bytes<byte> GetFixedList(NativeArray<byte> data)
        {
            FixedList4096Bytes<byte> retVal = new FixedList4096Bytes<byte>();
            unsafe
            {
                retVal.AddRange(NativeArrayUnsafeUtility.GetUnsafePtr(data), data.Length);
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
        /// The network pipeline to stream data.
        /// </summary>
        public NetworkPipeline pipeline;

        /// <summary>
        /// The client's network connection instance.
        /// </summary>
        public Unity.Networking.Transport.NetworkConnection connection;

        /// <summary>
        /// The segment of data to send over (deallocates after use).
        /// </summary>
        [DeallocateOnJobCompletion]
        public NativeArray<byte> data;

        public void Execute()
        {
            DataStreamWriter writer;
            int writeStatus = driver.BeginSend(pipeline, connection, out writer);

            if (writeStatus == (int)Unity.Networking.Transport.Error.StatusCode.Success)
            {
                writer.WriteBytes(data);
                driver.EndSend(writer);
            }
        }
    }

    #endregion

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

        /// <summary>
        /// The driver's max header size for UTP transport.
        /// </summary>
        private int[] driverMaxHeaderSize;

        /// <summary>
        /// Whether the client is connected to the server or not.
        /// </summary>
        private bool connected;

        public UtpClient(int timeoutInMilliseconds)
        {
            this.timeout = timeoutInMilliseconds;

            //Allocate max header size array 
            driverMaxHeaderSize = new int[2];
        }

        public UtpClient(Action OnConnected, Action<ArraySegment<byte>> OnReceivedData, Action OnDisconnected, int timeout)
            : this(timeout)
        {
            this.OnConnected = OnConnected;
            this.OnReceivedData = OnReceivedData;
            this.OnDisconnected = OnDisconnected;
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
                UtpLog.Warning("Client is already connected");
                return;
            }

            var settings = new NetworkSettings();
            settings.WithNetworkConfigParameters(disconnectTimeoutMS: timeout);

            driver = NetworkDriver.Create(settings);
            reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            unreliablePipeline = driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            connectionEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);

            if (host == "localhost")
            {
                host = "127.0.0.1";
            }

            NetworkEndPoint endpoint = NetworkEndPoint.Parse(host, port); // TODO: also support IPV6
            connection = driver.Connect(endpoint);

            UtpLog.Info("Client connecting to server at: " + endpoint.Address);
        }

        /// <summary>
        /// Attempt to connect to a Relay host given a join allocation.
        /// </summary>
        /// <param name="joinAllocation"></param>
		public void RelayConnect(JoinAllocation joinAllocation)
        {
            if (IsConnected())
            {
                UtpLog.Warning("Client is already connected");
                return;
            }

            RelayServerData relayServerData = RelayUtils.PlayerRelayData(joinAllocation, "udp");
            RelayNetworkParameter relayNetworkParameter = new RelayNetworkParameter { ServerData = relayServerData };
            NetworkSettings networkSettings = new NetworkSettings();
            RelayParameterExtensions.WithRelayParameters(ref networkSettings, ref relayServerData);

            driver = NetworkDriver.Create(networkSettings);
            reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            unreliablePipeline = driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            connectionEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);

            connection = driver.Connect(relayNetworkParameter.ServerData.Endpoint);

            UtpLog.Info("Client connecting to server at: " + relayNetworkParameter.ServerData.Endpoint.Address);
        }

        /// <summary>
        /// Whether or not the client is connected to a server.
        /// </summary>
        /// <returns>True if connected to a server, false otherwise.</returns>
        public bool IsConnected()
        {
            return connected;
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
                UtpLog.Info("Disconnecting from server");

                connection.Disconnect(driver);
                connection = default(Unity.Networking.Transport.NetworkConnection);

                // When disconnecting, we need to ensure the driver has the opportunity to send a disconnect event to the server
                driver.ScheduleUpdate().Complete();

                OnDisconnected?.Invoke();
            }

            if (connectionEventsQueue.IsCreated)
            {
                ProcessIncomingEvents(); // Ensure we flush the queue
                connectionEventsQueue.Dispose();
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

            //Cache driver & connection info
            CacheConnectionInfo();

            // Need to ensure the driver did not become inactive
            if (!DriverActive())
            {
                driverMaxHeaderSize = new int[2];
                return;
            }

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
        /// Caches important properties to allow for getter methods to be called without interfering with the job system.
        /// </summary>
        private void CacheConnectionInfo()
        {
            //Check for an active connection from this client
            if (connection != default(Unity.Networking.Transport.NetworkConnection))
            {
                //If driver is active, cache its max header size for UTP transport
                if (DriverActive())
                {
                    driverMaxHeaderSize[Channels.Reliable] = driver.MaxHeaderSize(reliablePipeline);
                    driverMaxHeaderSize[Channels.Unreliable] = driver.MaxHeaderSize(unreliablePipeline);
                }

                //Set connection state
                connected = DriverActive() && connection.GetState(driver) == Unity.Networking.Transport.NetworkConnection.State.Connected;
            }
            else
            {
                //If there is no valid connection, set values accordingly
                driverMaxHeaderSize[Channels.Reliable] = 0;
                driverMaxHeaderSize[Channels.Unreliable] = 0;
                connected = false;
            }
        }

        /// <summary>
        /// Send data to the listen server over a particular channel.
        /// </summary>
        /// <param name="segment">The data to send.</param>
        /// <param name="channelId">The 'Mirror.Channels' channel to send the data over.</param>
        public void Send(ArraySegment<byte> segment, int channelId)
        {
            //Get pipeline for job
            NetworkPipeline pipeline = channelId == Channels.Reliable ? reliablePipeline : unreliablePipeline;

            //Convert ArraySegment to NativeArray for burst compile
            NativeArray<byte> segmentArray = new NativeArray<byte>(segment.Count, Allocator.Persistent);
            NativeArray<byte>.Copy(segment.Array, segment.Offset, segmentArray, 0, segment.Count);

            // Create a new job
            var job = new ClientSendJob
            {
                driver = driver,
                pipeline = pipeline,
                connection = connection,
                data = segmentArray
            };

            // Schedule job
            clientJobHandle = job.Schedule(clientJobHandle);
        }

        /// <summary>
        /// Processes connection events from the queue.
        /// </summary>
        public void ProcessIncomingEvents()
        {
            // Exit if the driver is not active
            if (!DriverActive())
                return;

            // Exit if the connection is not ready
            if (!connection.IsCreated || !connection.IsCreated)
                return;

            UtpConnectionEvent connectionEvent;
            while (connectionEventsQueue.IsCreated && connectionEventsQueue.TryDequeue(out connectionEvent))
            {
                if (connectionEvent.eventType == (byte)UtpConnectionEventType.OnConnected)
                {
                    OnConnected?.Invoke();
                }
                else if (connectionEvent.eventType == (byte)UtpConnectionEventType.OnReceivedData)
                {
                    OnReceivedData?.Invoke(new ArraySegment<byte>(connectionEvent.eventData.ToArray()));
                }
                else if (connectionEvent.eventType == (byte)UtpConnectionEventType.OnDisconnected)
                {
                    OnDisconnected?.Invoke();
                }
                else
                {
                    UtpLog.Warning("invalid connection event: " + connectionEvent.eventType);
                }
            }
        }

        /// <summary>
        /// Returns this client's driver's max header size based on the requested channel.
        /// </summary>
        /// <param name="channelId">The channel to check.</param>
        /// <returns>This client's max header size.</returns>
        public int GetMaxHeaderSize(int channelId = Channels.Reliable)
        {
            return DriverActive() ? driverMaxHeaderSize[channelId] : 0;
        }
    }
}