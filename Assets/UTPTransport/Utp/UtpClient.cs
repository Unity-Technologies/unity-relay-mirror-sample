using Mirror;

using System;

using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;

namespace Utp
{
    struct ClientUpdateJob : IJob
    {
        /// <summary>
        /// Used to bind, listen, and send data to connections.
        /// </summary>
        public NetworkDriver driver;

        /// <summary>
        /// client connections to this server.
        /// </summary>
        public NativeArray<Unity.Networking.Transport.NetworkConnection> connection;

        /// <summary>
        /// Temporary storage for connection events that occur on job threads so they may be dequeued on the main thread.
        /// </summary>
        public NativeQueue<UtpConnectionEvent>.ParallelWriter connectionEventsQueue;

        /// <summary>
        /// Process all incoming events/messages on this connection.
        /// </summary>
        public void Execute()
        {
            if (!connection[0].IsCreated)
            {
                return;
            }

            DataStreamReader stream;
            NetworkEvent.Type netEvent;
            while ((netEvent = connection[0].PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
            {
                if (netEvent == NetworkEvent.Type.Connect)
                {
                    UtpLog.Info("Client successfully connected to server");

                    UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
                    connectionEvent.eventType = (byte)UtpConnectionEventType.OnConnected;
                    connectionEvent.connectionId = connection[0].GetHashCode();

                    connectionEventsQueue.Enqueue(connectionEvent);
                }
                else if (netEvent == NetworkEvent.Type.Data)
                {
                    NativeArray<byte> nativeMessage = new NativeArray<byte>(stream.Length, Allocator.Temp);
                    stream.ReadBytes(nativeMessage);

                    UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
                    connectionEvent.eventType = (byte)UtpConnectionEventType.OnReceivedData;
                    connectionEvent.connectionId = connection[0].GetHashCode();
					connectionEvent.eventData = GetFixedList(nativeMessage);

					connectionEventsQueue.Enqueue(connectionEvent);
                }
                else if (netEvent == NetworkEvent.Type.Disconnect)
                {
                    UtpLog.Info("Client disconnected from server");

                    UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
                    connectionEvent.eventType = (byte)UtpConnectionEventType.OnDisconnected;
                    connectionEvent.connectionId = connection[0].GetHashCode();

                    connectionEventsQueue.Enqueue(connectionEvent);
                }
                else
                {
                    UtpLog.Warning("Received unknown event: " + netEvent);
                }
            }
        }

        public FixedList4096Bytes<byte> GetFixedList(NativeArray<byte> data)
        {
            FixedList4096Bytes<byte> retVal = new FixedList4096Bytes<byte>();
            foreach (byte dataByte in data)
            {
                retVal.Add(dataByte);
            }
            return retVal;
        }
    }

    /// <summary>
    /// A client for Mirror using UTP.
    /// </summary>
    public class UtpClient : CoroutineWrapper
	{
		// Events
		public Action OnConnected;
		public Action<ArraySegment<byte>> OnReceivedData;
		public Action OnDisconnected;

        /// <summary>
        /// Temporary storage for connection events that occur on job threads so they may be dequeued on the main thread.
        /// </summary>
        private NativeQueue<UtpConnectionEvent> m_ConnectionEventsQueue;

        /// <summary>
        /// Used alongside a connection to connect, send, and receive data from a listen server.
        /// </summary>
        private NetworkDriver m_Driver;

        /// <summary>
        /// Used alongside a driver to connect, send, and receive data from a listen server.
        /// </summary>
        private NativeArray<Unity.Networking.Transport.NetworkConnection> m_Connection;

		/// <summary>
		/// A pipeline on the driver that is sequenced, and ensures messages are delivered.
		/// </summary>
		private NetworkPipeline m_ReliablePipeline;

		/// <summary>
		/// A pipeline on the driver that is sequenced, but does not ensure messages are delivered.
		/// </summary>
		private NetworkPipeline m_UnreliablePipeline;

        /// <summary>
        /// Job handle to schedule client jobs.
        /// </summary>
		private JobHandle m_ClientJobHandle;

        /// <summary>
        /// Timeout(ms) to be set on drivers.
        /// </summary>
        private int m_Timeout;

		public UtpClient(Action OnConnected, Action<ArraySegment<byte>> OnReceivedData, Action OnDisconnected, int timeout)
		{
			this.OnConnected = OnConnected;
			this.OnReceivedData = OnReceivedData;
			this.OnDisconnected = OnDisconnected;
            this.m_Timeout = timeout;
		}

		/// <summary>
		/// Attempt to connect to a listen server at a given IP/port. Currently only supports IPV4.
		/// </summary>
		/// <param name="host">The host address at which the listen server is running.</param>
		/// <param name="port">The port which the listen server is listening on.</param>
		public void Connect(string host, ushort port)
		{
            m_ClientJobHandle.Complete();

			if (IsConnected())
			{
				UtpLog.Warning("Client is already connected");
				return;
            }

            var settings = new NetworkSettings();
            settings.WithNetworkConfigParameters(disconnectTimeoutMS: m_Timeout);

            m_Driver = NetworkDriver.Create(settings);
            m_ReliablePipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            m_UnreliablePipeline = m_Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            m_Connection = new NativeArray<Unity.Networking.Transport.NetworkConnection>(1, Allocator.Persistent);
            m_ConnectionEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);

			if (host == "localhost")
			{
				host = "127.0.0.1";
			}

			NetworkEndPoint endpoint = NetworkEndPoint.Parse(host, port); // TODO: also support IPV6
			m_Connection[0] = m_Driver.Connect(endpoint);

			UtpLog.Info("Client connecting to server at: " + endpoint.Address);
		}

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
			networkSettings.AddRawParameterStruct(ref relayNetworkParameter);

			m_Driver = NetworkDriver.Create(networkSettings);
			m_ReliablePipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
			m_UnreliablePipeline = m_Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
			m_Connection = new NativeArray<Unity.Networking.Transport.NetworkConnection>(1, Allocator.Persistent);
			m_ConnectionEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);

			m_Connection[0] = m_Driver.Connect(relayNetworkParameter.ServerData.Endpoint);

			UtpLog.Info("Client connecting to server at: " + relayNetworkParameter.ServerData.Endpoint.Address);
		}

		/// <summary>
		/// Whether or not the client is connected to a server.
		/// </summary>
		/// <returns>True if connected to a server, false otherwise.</returns>
		public bool IsConnected()
		{
			return DriverActive() &&
				m_Connection[0].GetState(m_Driver) == Unity.Networking.Transport.NetworkConnection.State.Connected;
		}

		/// <summary>
		/// Whether or not the network driver has been initialized.
		/// </summary>
		/// <returns>True if initialized, false otherwise.</returns>
		private bool DriverActive()
		{
			return !Equals(m_Driver, default(NetworkDriver));
		}

		/// <summary>
		/// Disconnect from a listen server.
		/// </summary>
		public void Disconnect()
		{
            m_ClientJobHandle.Complete();

            if (m_Connection.IsCreated)
			{
                UtpLog.Info("Disconnecting from server");

				m_Connection[0].Disconnect(m_Driver);
				// When disconnecting, we need to ensure the driver has the opportunity to send a disconnect event to the server
				m_Driver.ScheduleUpdate().Complete();

				OnDisconnected.Invoke();
            }

			if (m_ConnectionEventsQueue.IsCreated)
			{
				ProcessIncomingEvents(); // Ensure we flush the queue
				m_ConnectionEventsQueue.Dispose();
			}

			if (m_Connection.IsCreated)
			{
				m_Connection.Dispose();
			}

			if (m_Driver.IsCreated)
			{
				m_Driver.Dispose();
				m_Driver = default(NetworkDriver);
			}
		}

		/// <summary>
		/// Tick the client, creating the client job and scheduling it. Processes incoming events 
		/// </summary>
		public void Tick()
		{
            // First complete the job that was initialized in the previous frame
            m_ClientJobHandle.Complete();

            // Trigger Mirror callbacks for events that resulted in the last jobs work
            ProcessIncomingEvents();

            // Need to ensure the driver did not become inactive
            if (!DriverActive())
                return;

            // Create a new job
            var job = new ClientUpdateJob
            {
                driver = m_Driver,
                connection = m_Connection,
				connectionEventsQueue = m_ConnectionEventsQueue.AsParallelWriter()
            };

            // Schedule job
            m_ClientJobHandle = m_Driver.ScheduleUpdate();
            m_ClientJobHandle = job.Schedule(m_ClientJobHandle);
        }

        /// <summary>
        /// Send data to the listen server over a particular channel.
        /// </summary>
        /// <param name="segment">The data to send.</param>
        /// <param name="channelId">The 'Mirror.Channels' channel to send the data over.</param>
        public void Send(ArraySegment<byte> segment, int channelId)
		{
            m_ClientJobHandle.Complete();

            System.Type stageType = channelId == Channels.Reliable ? typeof(ReliableSequencedPipelineStage) : typeof(UnreliableSequencedPipelineStage);
            NetworkPipeline pipeline = channelId == Channels.Reliable ? m_ReliablePipeline : m_UnreliablePipeline;

            NetworkPipelineStageId stageId = NetworkPipelineStageCollection.GetStageId(stageType);
			m_Driver.GetPipelineBuffers(pipeline, stageId, m_Connection[0], out var tmpReceiveBuffer, out var tmpSendBuffer, out var reliableBuffer);

			DataStreamWriter writer;
			int writeStatus = m_Driver.BeginSend(pipeline, m_Connection[0], out writer);
			if (writeStatus == 0)
			{
				// segment.Array is longer than the number of bytes it holds, grab just what we need
				byte[] segmentArray = new byte[segment.Count];
				Array.Copy(segment.Array, 0, segmentArray, 0, segment.Count);

				NativeArray<byte> nativeMessage = new NativeArray<byte>(segmentArray, Allocator.Temp);
				writer.WriteBytes(nativeMessage);
				m_Driver.EndSend(writer);
			}
			else
			{
				UtpLog.Warning("Write not successful: " + writeStatus);
			}
		}

		public void ProcessIncomingEvents()
        {
            // Exit if the driver is not active
            if (!DriverActive() || !NetworkClient.active)
                return;

            // Exit if the connection is not ready
            if (!m_Connection.IsCreated || !m_Connection[0].IsCreated)
                return;

            UtpConnectionEvent connectionEvent;
            while (m_ConnectionEventsQueue.IsCreated && m_ConnectionEventsQueue.TryDequeue(out connectionEvent))
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
					UtpLog.Warning("invalid connection event: " + connectionEvent.eventType);
                }
            }
        }
	}
}