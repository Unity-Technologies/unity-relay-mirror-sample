using System;
using System.Collections.Generic;

using Mirror;

using Unity.Jobs;
using Unity.Collections;
using Unity.Networking.Transport;

namespace UtpTransport
{
    struct ServerUpdateConnectionsJob : IJob
    {
        /// <summary>
        /// Used to bind, listen, and send data to connections.
        /// </summary>
        public NetworkDriver driver;

        /// <summary>
        /// client connections to this server.
        /// </summary>
        public NativeList<Unity.Networking.Transport.NetworkConnection> connections;

        /// <summary>
        /// Stores connection events as they come up, to be ran in the main thread later.
        /// </summary>
        public NativeQueue<UtpConnectionEvent>.ParallelWriter eventsQueue;

        public void Execute()
        {
            // Clean up connections
            {
                HashSet<int> connectionsToRemove = new HashSet<int>();
                foreach (Unity.Networking.Transport.NetworkConnection connection in connections)
                {
                    if (!connection.IsCreated)
                    {
                        connectionsToRemove.Add(connection.GetHashCode());
                    }
                    /*else if (connection.m_TimedOut)
                    {
                        UtpLog.Info("Client has timed out. Connection ID: " + connection);
                        Disconnect(connection.GetHashCode());
                        connectionsToRemove.Add(connection.GetHashCode());
                    }*/
                }

                foreach (int connectionId in connectionsToRemove)
                {
                    UtpLog.Info("Removing connection with ID: " + connectionId);
                    connections.RemoveAt(connectionId);
                }
                connectionsToRemove.Clear();
            }

            // Accept new connections
            {
                Unity.Networking.Transport.NetworkConnection networkConnection;
                while ((networkConnection = driver.Accept()) != default(Unity.Networking.Transport.NetworkConnection))
                {
                    UtpLog.Info("Adding connection with ID: " + networkConnection.GetHashCode());
                    Unity.Networking.Transport.NetworkConnection toAdd = networkConnection;

                    UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
                    connectionEvent.eventType = (byte)UtpConnectionEventType.OnConnected;
                    connectionEvent.connectionId = toAdd.GetHashCode();
                    eventsQueue.Enqueue(connectionEvent);

                    connections.Add(toAdd);
                }
            }
        }

        /// <summary>
        /// Disconnect and remove a connection via it's ID.
        /// </summary>
        /// <param name="connectionId">The ID of the connection to disconnect.</param>
        public void Disconnect(int connectionId)
        {
            foreach (Unity.Networking.Transport.NetworkConnection connection in connections)
            {
                if (connection.GetHashCode() == connectionId)
                {
                    UtpLog.Info("Disconnecting connection with ID: " + connectionId);
                    connection.Disconnect(driver);
                    UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
                    connectionEvent.eventType = (byte)UtpConnectionEventType.OnDisconnected;
                    connectionEvent.connectionId = connection.GetHashCode();
                    eventsQueue.Enqueue(connectionEvent);
                }
            }
        }
    }

    struct ServerUpdateJob : IJobParallelForDefer
    {
        /// <summary>
        /// Used to bind, listen, and send data to connections.
        /// </summary>
        public NetworkDriver.Concurrent driver;

        /// <summary>
        /// client connections to this server.
        /// </summary>
        public NativeArray<Unity.Networking.Transport.NetworkConnection> connections;

        /// <summary>
        /// Stores connection events as they come up, to be ran in the main thread later.
        /// </summary>
        public NativeQueue<UtpConnectionEvent>.ParallelWriter eventsQueue;

        /// <summary>
        /// Process all incoming events/messages on this connection.
        /// </summary>
        /// <param name="index">The current index being accessed in the array.</param>
        public void Execute(int index)
        {
            DataStreamReader stream;
            NetworkEvent.Type netEvent;
            while ((netEvent = driver.PopEventForConnection(connections[index], out stream)) != NetworkEvent.Type.Empty)
            {
                if (netEvent == NetworkEvent.Type.Data)
                {
                    NativeArray<byte> nativeMessage = new NativeArray<byte>(stream.Length, Allocator.Temp);
                    stream.ReadBytes(nativeMessage);

                    UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
                    connectionEvent.eventType = (byte)UtpConnectionEventType.OnReceivedData;
                    connectionEvent.eventData = GetFixedList(nativeMessage);
                    connectionEvent.connectionId = connections[index].GetHashCode();
                    eventsQueue.Enqueue(connectionEvent);
                }
                else if (netEvent == NetworkEvent.Type.Disconnect)
                {
                    UtpLog.Verbose("Client disconnected from server");

                    UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
                    connectionEvent.eventType = (byte)UtpConnectionEventType.OnDisconnected;
                    connectionEvent.connectionId = connections[index].GetHashCode();

                    eventsQueue.Enqueue(connectionEvent);
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
    /// A listen server for Mirror using UTP. 
    /// </summary>
    public class UtpServer
    {
        // Events
        public Action<int> OnConnected;
        public Action<int, ArraySegment<byte>> OnReceivedData;
        public Action<int> OnDisconnected;

        /// <summary>
        /// Stores connection events as they come up, to be ran in the main thread later.
        /// </summary>
        public NativeQueue<UtpConnectionEvent> connectionsEventsQueue;

        /// <summary>
        /// Used to bind, listen, and send data to connections.
        /// </summary>
        private NetworkDriver m_Driver;

        /// <summary>
        /// client connections to this server.
        /// </summary>
        public NativeList<Unity.Networking.Transport.NetworkConnection> connections;

        /// <summary>
        /// Job handle to schedule server jobs.
        /// </summary>
        private JobHandle ServerJobHandle;

        /// <summary>
        /// A pipeline on the driver that is sequenced, and ensures messages are delivered.
        /// </summary>
        private NetworkPipeline m_ReliablePipeline;

        /// <summary>
        /// A pipeline on the driver that is sequenced, but does not ensure messages are delivered.
        /// </summary>
        private NetworkPipeline m_UnreliablePipeline;

        public UtpServer(Action<int> OnConnected,
            Action<int, ArraySegment<byte>> OnReceivedData,
            Action<int> OnDisconnected)
        {
            this.OnConnected = OnConnected;
            this.OnReceivedData = OnReceivedData;
            this.OnDisconnected = OnDisconnected;
        }

        /// <summary>
        /// Initialize the server. Currently only supports IPV4.
        /// </summary>
        /// <param name="port">The port to listen for connections on.</param>
        public void Start(ushort port)
        {
            if (IsActive())
            {
                UtpLog.Warning("Server already active");
                return;
            }

            m_Driver = NetworkDriver.Create();
            connections = new NativeList<Unity.Networking.Transport.NetworkConnection>(16, Allocator.Persistent);
            connectionsEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);
            m_ReliablePipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            m_UnreliablePipeline = m_Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));

            NetworkEndPoint endpoint = NetworkEndPoint.AnyIpv4;
            endpoint.Port = port;
            if (m_Driver.Bind(endpoint) != 0)
            {
                UtpLog.Error("Failed to bind to port: " + endpoint.Port);
            }
            else
            {
                m_Driver.Listen();
            }

            UtpLog.Info("Server started on port: " + endpoint.Port);
        }

        /// <summary>
        /// Tick the server, creating the server jobs and scheduling them. Processes events created by the jobs.
        /// </summary>
        public void Tick()
        {
            if (!IsActive())
                return;

            ServerJobHandle.Complete();

            ProcessIncomingEvents();

            var connectionJob = new ServerUpdateConnectionsJob
            {
                driver = m_Driver,
                connections = connections,
                eventsQueue = connectionsEventsQueue.AsParallelWriter()
            };

            // Query incoming events for all connections
            var serverUpdateJob = new ServerUpdateJob
            {
                driver = m_Driver.ToConcurrent(),
                connections = connections.AsDeferredJobArray(),
                eventsQueue = connectionsEventsQueue.AsParallelWriter()
            };

            ServerJobHandle = m_Driver.ScheduleUpdate();
            ServerJobHandle = connectionJob.Schedule(ServerJobHandle);
            ServerJobHandle = serverUpdateJob.Schedule(connections, 1, ServerJobHandle);
        }

        /// <summary>
        /// Stop a running server.
        /// </summary>
        public void Stop()
        {
            UtpLog.Info("Stopping server");

            ServerJobHandle.Complete();
            m_Driver.Dispose();
            connections.Dispose();
            connectionsEventsQueue.Dispose();
            m_Driver = default(NetworkDriver);
        }

        /// <summary>
        /// Disconnect and remove a connection via it's ID.
        /// </summary>
        /// <param name="connectionId">The ID of the connection to disconnect.</param>
        public void Disconnect(int connectionId)
        {
            foreach (Unity.Networking.Transport.NetworkConnection connection in connections)
            {
                if (connection.GetHashCode() == connectionId)
                {
                    UtpLog.Info("Disconnecting connection with ID: " + connectionId);
                    connection.Disconnect(m_Driver);
                    OnDisconnected(connectionId);
                }
            }
        }

        /// <summary>
        /// Send data to a connection over a particular channel.
        /// </summary>
        /// <param name="connectionId">The ID of the connection to send data to.</param>
        /// <param name="segment">The data to send.</param>
        /// <param name="channelId">The 'Mirror.Channels' channel to send the data over.</param>
        public void Send(int connectionId, ArraySegment<byte> segment, int channelId)
        {
            ServerJobHandle.Complete();

            System.Type stageType = channelId == Channels.Reliable ? typeof(ReliableSequencedPipelineStage) : typeof(UnreliableSequencedPipelineStage);
            NetworkPipeline pipeline = channelId == Channels.Reliable ? m_ReliablePipeline : m_UnreliablePipeline;

            foreach(Unity.Networking.Transport.NetworkConnection connection in connections)
            {
                if(connection.GetHashCode() == connectionId)
                {
                    NetworkPipelineStageId stageId = NetworkPipelineStageCollection.GetStageId(stageType);
                    m_Driver.GetPipelineBuffers(pipeline, stageId, connection, out var tmpReceiveBuffer, out var tmpSendBuffer, out var reliableBuffer);

                    DataStreamWriter writer;
                    int writeStatus = m_Driver.BeginSend(pipeline, connection, out writer);
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
            }
        }

        /// <summary>
        /// Look up a client's address via it's ID.
        /// </summary>
        /// <param name="connectionId">The ID of the connection.</param>
        /// <returns>The client address.</returns>
        public string GetClientAddress(int connectionId)
        {
            foreach (Unity.Networking.Transport.NetworkConnection connection in connections)
            {
                if (connection.GetHashCode() == connectionId)
                {
                    NetworkEndPoint endpoint = m_Driver.RemoteEndPoint(connection);
                    return endpoint.Address;
                }
            }
            return "";
        }

        /// <summary>
        /// Determine whether the server is running or not.
        /// </summary>
        /// <returns>True if running, false otherwise.</returns>
        public bool IsActive()
        {
            return !Equals(m_Driver, default(NetworkDriver));
        }

        /// <summary>
        /// Processes connection events from the queue.
        /// </summary>
        public void ProcessIncomingEvents()
        {
            UtpConnectionEvent connectionEvent;
            while (connectionsEventsQueue.TryDequeue(out connectionEvent))
            {
                foreach (Unity.Networking.Transport.NetworkConnection connection in connections)
                {
                    int eventId = connectionEvent.connectionId;
                    int connectionId = connection.GetHashCode();
                    if (connection.GetHashCode() == connectionEvent.connectionId)
                    {
                        if (connectionEvent.eventType == (byte)UtpConnectionEventType.OnConnected)
                        {
                            OnConnected.Invoke(connectionEvent.connectionId);
                        }
                        else if (connectionEvent.eventType == (byte)UtpConnectionEventType.OnReceivedData)
                        {
                            OnReceivedData.Invoke(connectionEvent.connectionId, new ArraySegment<byte>(connectionEvent.eventData.ToArray()));
                        }
                        else if (connectionEvent.eventType == (byte)UtpConnectionEventType.OnDisconnected)
                        {
                            OnDisconnected.Invoke(connectionEvent.connectionId);
                        }
                        else
                        {
                            UtpLog.Info("invalid connection event: " + connectionEvent.eventType);
                        }
                    }
                }
            }
        }
    }
}
