using System;

using Mirror;

using Unity.Jobs;
using Unity.Collections;
using Unity.Networking.Transport;

namespace UtpTransport
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
        /// Stores connection events as they come up, to be ran in the main thread later.
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
                    UtpLog.Info("Receiving data");
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
    public class UtpClient
	{
		// Events
		public Action OnConnected;
		public Action<ArraySegment<byte>> OnReceivedData;
		public Action OnDisconnected;

        /// <summary>
        /// Stores connection events as they come up, to be ran in the main thread later.
        /// </summary>
        public NativeQueue<UtpConnectionEvent> connectionEventsQueue;

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
		public JobHandle ClientJobHandle;

		public UtpClient(Action OnConnected, Action<ArraySegment<byte>> OnReceivedData, Action OnDisconnected)
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
			if (IsConnected())
			{
				UtpLog.Warning("Client is already connected");
				return;
            }

            m_Connection = new NativeArray<Unity.Networking.Transport.NetworkConnection>(1, Allocator.Persistent);
            m_Driver = NetworkDriver.Create();
            connectionEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);
            m_ReliablePipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
			m_UnreliablePipeline = m_Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));

			if (host == "localhost")
			{
				host = "127.0.0.1";
			}

			NetworkEndPoint endpoint = NetworkEndPoint.Parse(host, port); // TODO: also support IPV6
			m_Connection[0] = m_Driver.Connect(endpoint);

			UtpLog.Info("Client connecting to server at: " + endpoint.Address);
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
            ClientJobHandle.Complete();
            m_Driver.Dispose();

            if (m_Connection.IsCreated)
            {
                m_Connection.Dispose();
            }

            if (connectionEventsQueue.IsCreated)
            {
                connectionEventsQueue.Dispose();
            }

			m_Driver = default(NetworkDriver);
		}

		/// <summary>
		/// Tick the client, creating the client job and scheduling it. Processes incoming events 
		/// </summary>
		public void Tick()
		{
			if (!DriverActive())
				return;

            ClientJobHandle.Complete();

            ProcessIncomingEvents();

            var job = new ClientUpdateJob
            {
                driver = m_Driver,
                connection = m_Connection,
				connectionEventsQueue = connectionEventsQueue.AsParallelWriter()
            };

            ClientJobHandle = m_Driver.ScheduleUpdate();
            ClientJobHandle = job.Schedule(ClientJobHandle);
        }

        /// <summary>
        /// Send data to the listen server over a particular channel.
        /// </summary>
        /// <param name="segment">The data to send.</param>
        /// <param name="channelId">The 'Mirror.Channels' channel to send the data over.</param>
        public void Send(ArraySegment<byte> segment, int channelId)
		{
            ClientJobHandle.Complete();

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
            // Exit if the connection is not ready
            if (!m_Connection[0].IsCreated)
            {
                return;
            }

            UtpConnectionEvent connectionEvent;
            while (connectionEventsQueue.TryDequeue(out connectionEvent))
            {
				if(connectionEvent.eventType == (byte)UtpConnectionEventType.OnConnected)
                {
					OnConnected.Invoke();
                }
				else if(connectionEvent.eventType == (byte)UtpConnectionEventType.OnReceivedData)
                {
					OnReceivedData.Invoke(new ArraySegment<Byte>(connectionEvent.eventData.ToArray()));
                }
				else if(connectionEvent.eventType == (byte)UtpConnectionEventType.OnDisconnected)
                {
					OnDisconnected.Invoke();
                }
                else
                {
					UtpLog.Info("invalid connection event: " + connectionEvent.eventType);
                }
            }
        }
	}

}