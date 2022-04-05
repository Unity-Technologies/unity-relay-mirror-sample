using Mirror;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

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
            //Back out if connection is invalid
            if (!connection.IsCreated) return;

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
            //Back out if connection is invalid
            if(!connection.IsCreated) return;

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
    public class UtpClient : UtpEntity
	{
		/// <summary>
        /// Invokes when connected to a server.
        /// </summary>
		public Action OnConnected;

        /// <summary>
        /// Invokes when data has been received.
        /// </summary>
		public Action<ArraySegment<byte>> OnReceivedData;

        /// <summary>
        /// Invokes when disconnected from a server.
        /// </summary>
		public Action OnDisconnected;

        /// <summary>
        /// Used alongside a driver to connect, send, and receive data from a listen server.
        /// </summary>
        private Unity.Networking.Transport.NetworkConnection connection;

        /// <summary>
        /// The driver's max header size for UTP transport.
        /// </summary>
        private int[] driverMaxHeaderSize = new int[2];

        /// <summary>
        /// Whether the client is connected to the server or not.
        /// </summary>
        private bool connected;

        /// <summary>
        /// Constructor for UTP client.
        /// </summary>
        /// <param name="OnConnected">Action that is invoked when connected.</param>
        /// <param name="OnReceivedData">Action that is invoked when receiving data.</param>
        /// <param name="OnDisconnected">Action that is invoked when disconnected.</param>
        /// <param name="timeout">The reponse timeout, in miliseconds.</param>
		public UtpClient(Action OnConnected, Action<ArraySegment<byte>> OnReceivedData, Action OnDisconnected, int timeout)
		{
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
            jobHandle.Complete();

            //Check for double connection
			if (IsConnected())
			{
				UtpLog.Warning("Client is already connected");
				return;
            }

            //Check for blank host
            if(host == "")
            {
                UtpLog.Error("Client attmepted to connect to empty host");
                return;
            }

            //Set localhost to local IP
            if (host == "localhost")
            {
                host = "127.0.0.1";
            }

            //Initialize network settings
            var settings = new NetworkSettings();
            settings.WithNetworkConfigParameters(disconnectTimeoutMS: timeout);

            //Instiantate network driver
            driver = NetworkDriver.Create(settings);

            //Instantiate event queue
            connectionsEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);

            //Create network pipelines
            reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            unreliablePipeline = driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));

            //Attempt endpoint connection
			NetworkEndPoint endpoint = NetworkEndPoint.Parse(host, port); // TODO: also support IPV6
			connection = driver.Connect(endpoint);
            var address = endpoint.Address;

            //No response on endpoint connection
            if (!ConnectionIsActive(connection))
            {
                UtpLog.Error($"Client failed to connect to server at {address}");
                return;
            }

            //Successfull connection
            UtpLog.Info($"Client connecting to server at {address}");
        }

        /// <summary>
        /// Attempt to connect to a Relay host given a join allocation.
        /// </summary>
        /// <param name="joinAllocation"></param>
		public void RelayConnect(JoinAllocation joinAllocation)
		{
            //Check for existing connection status
			if (IsConnected())
			{
				UtpLog.Warning("Client is already connected");
				return;
			}

            //Instantiate relay network data
			RelayServerData relayServerData = RelayUtils.PlayerRelayData(joinAllocation, "udp");
			RelayNetworkParameter relayNetworkParameter = new RelayNetworkParameter { ServerData = relayServerData };
			NetworkSettings networkSettings = new NetworkSettings();

            //Initialize relay network
            RelayParameterExtensions.WithRelayParameters(ref networkSettings, ref relayServerData);

            //Instiantate network driver
            driver = NetworkDriver.Create(networkSettings);

            //Instantiate event queue
            connectionsEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);

            //Create network pipelines
            reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
			unreliablePipeline = driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));

            //Attempt relay connection
			connection = driver.Connect(relayNetworkParameter.ServerData.Endpoint);
            var address = relayNetworkParameter.ServerData.Endpoint.Address;

            //No response on endpoint connection
            if (!ConnectionIsActive(connection))
            {
                UtpLog.Error($"Client failed to connect to Relay server at {address}");
                return;
            }

            //Successfull connection
            UtpLog.Info($"Client connecting to Relay server at {address}");
        }

		/// <summary>
		/// Disconnect from a listen server.
		/// </summary>
		public void Disconnect()
		{
            jobHandle.Complete();

            //If there is an existing connection, force a disconnect
            if (connection.IsCreated)
			{
                UtpLog.Info("Client disconnecting from server");

                //Disconnect from server
				connection.Disconnect(driver);
                connection = default(Unity.Networking.Transport.NetworkConnection);

				//We need to ensure the driver has the opportunity to send a disconnect event to the server
				driver.ScheduleUpdate().Complete();

                //Invoke disconnect action
				OnDisconnected.Invoke();
            }

            //Flush the event queue
			if (connectionsEventsQueue.IsCreated)
			{
				ProcessIncomingEvents();
                connectionsEventsQueue.Dispose();
			}

            //Dispose of existing network driver
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
            jobHandle.Complete();

            // Trigger Mirror callbacks for events that resulted in the last jobs work
            ProcessIncomingEvents();

            //Cache driver & connection info
            CacheConnectionInfo();

            // Need to ensure the driver did not become inactive
            if (!IsActive())
            {
                driverMaxHeaderSize = new int[2];
                return;
            }

            // Create a new job
            var job = new ClientUpdateJob
            {
                driver = driver,
                connection = connection,
				connectionEventsQueue = connectionsEventsQueue.AsParallelWriter()
            };

            // Schedule job
            jobHandle = driver.ScheduleUpdate();
            jobHandle = job.Schedule(jobHandle);
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
            jobHandle = job.Schedule(jobHandle);
        }

        /// <summary>
        /// Processes connection events from the queue.
        /// </summary>
        public void ProcessIncomingEvents()
        {
            // Exit if the driver is not active
            if (!DriverIsActive(driver) || !NetworkClient.active)
                return;

            // Exit if the connection is not ready
            if (!connection.IsCreated)
                return;

            //Process event queue
            while (connectionsEventsQueue.IsCreated && connectionsEventsQueue.TryDequeue(out UtpConnectionEvent connectionEvent))
            {
                switch (connectionEvent.eventType)
                {
                    //Connect action 
                    case ((byte)UtpConnectionEventType.OnConnected):
                        OnConnected.Invoke();
                        break;

                    //Receive data action
                    case ((byte)UtpConnectionEventType.OnReceivedData):
                        OnReceivedData.Invoke(new ArraySegment<byte>(connectionEvent.eventData.ToArray()));
                        break;

                    //Disconnect action
                    case ((byte)UtpConnectionEventType.OnDisconnected):
                        OnDisconnected.Invoke();
                        break;

                    //Invalid action
                    default:
                        UtpLog.Warning($"Invalid connection event: {connectionEvent.eventType}");
                        break;
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
            return IsConnected() && IsActive() ? driverMaxHeaderSize[channelId] : 0;
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
        /// Caches important properties to allow for getter methods to be called without interfering with the job system.
        /// </summary>
        private void CacheConnectionInfo()
        {
            //Check for an active connection from this client
            if (!ConnectionIsActive(connection))
            {
                var driverIsActive = DriverIsActive(driver);

                //If driver is active, cache its max header size for UTP transport
                if (driverIsActive)
                {
                    driverMaxHeaderSize[Channels.Reliable] = driver.MaxHeaderSize(reliablePipeline);
                    driverMaxHeaderSize[Channels.Unreliable] = driver.MaxHeaderSize(unreliablePipeline);
                }

                //Set connection state
                connected = driverIsActive && connection.GetState(driver) == Unity.Networking.Transport.NetworkConnection.State.Connected;
            }
            else
            {
                //If there is no valid connection, set values accordingly
                driverMaxHeaderSize[Channels.Reliable] = 0;
                driverMaxHeaderSize[Channels.Unreliable] = 0;
                connected = false;
            }
        }
    }
}