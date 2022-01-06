using Mirror;

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;

namespace UtpTransport
{
    /// <summary>
    /// Job used to update connections. 
    /// </summary>
    struct ServerUpdateConnectionsJob : IJob
    {
        /// <summary>
        /// Used to bind, listen, and send data to connections.
        /// </summary>
        public NetworkDriver driver;

        /// <summary>
        /// Client connections to this server.
        /// </summary>
        public NativeList<Unity.Networking.Transport.NetworkConnection> connections;

        /// <summary>
        /// Temporary storage for connection events that occur on job threads so they may be dequeued on the main thread.
        /// </summary>
        public NativeQueue<UtpConnectionEvent>.ParallelWriter connectionsEventsQueue;

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

                    UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
                    connectionEvent.eventType = (byte)UtpConnectionEventType.OnConnected;
                    connectionEvent.connectionId = networkConnection.GetHashCode();
                    connectionsEventsQueue.Enqueue(connectionEvent);

                    connections.Add(networkConnection);
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
                    connectionsEventsQueue.Enqueue(connectionEvent);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Job to query incoming events for all connections. 
    /// </summary>
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
        /// Temporary storage for connection events that occur on job threads so they may be dequeued on the main thread.
        /// </summary>
        public NativeQueue<UtpConnectionEvent>.ParallelWriter connectionsEventsQueue;

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
                    connectionsEventsQueue.Enqueue(connectionEvent);
                }
                else if (netEvent == NetworkEvent.Type.Disconnect)
                {
                    UtpLog.Verbose("Client disconnected from server");

                    UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
                    connectionEvent.eventType = (byte)UtpConnectionEventType.OnDisconnected;
                    connectionEvent.connectionId = connections[index].GetHashCode();

                    connectionsEventsQueue.Enqueue(connectionEvent);
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
    public class UtpServer : CoroutineWrapper
    {
        // Events
        public Action<int> OnConnected;
        public Action<int, ArraySegment<byte>> OnReceivedData;
        public Action<int> OnDisconnected;

        /// <summary>
        /// Temporary storage for connection events that occur on job threads so they may be dequeued on the main thread.
        /// </summary>
        private NativeQueue<UtpConnectionEvent> m_ConnectionsEventsQueue;

        /// <summary>
        /// Used to bind, listen, and send data to connections.
        /// </summary>
        private NetworkDriver m_Driver;

        /// <summary>
        /// Client connections to this server.
        /// </summary>
        private NativeList<Unity.Networking.Transport.NetworkConnection> m_Connections;

        /// <summary>
        /// Job handle to schedule server jobs.
        /// </summary>
        private JobHandle m_ServerJobHandle;

        /// <summary>
        /// A pipeline on the driver that is sequenced, and ensures messages are delivered.
        /// </summary>
        private NetworkPipeline m_ReliablePipeline;

        /// <summary>
        /// A pipeline on the driver that is sequenced, but does not ensure messages are delivered.
        /// </summary>
        private NetworkPipeline m_UnreliablePipeline;

        /// <summary>
        /// Timeout(ms) to be set on drivers.
        /// </summary>
        private int m_Timeout;

        public UtpServer(Action<int> OnConnected,
            Action<int, ArraySegment<byte>> OnReceivedData,
            Action<int> OnDisconnected,
            int timeout)
        {
            this.OnConnected = OnConnected;
            this.OnReceivedData = OnReceivedData;
            this.OnDisconnected = OnDisconnected;
            this.m_Timeout = timeout;
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

            var settings = new NetworkSettings();
            settings.WithNetworkConfigParameters(disconnectTimeoutMS: m_Timeout);

            m_Driver = NetworkDriver.Create(settings);
            m_Connections = new NativeList<Unity.Networking.Transport.NetworkConnection>(16, Allocator.Persistent);
            m_ConnectionsEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);
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

            // First complete the job that was initialized in the previous frame
            m_ServerJobHandle.Complete();

            // Trigger Mirror callbacks for events that resulted in the last jobs work
            ProcessIncomingEvents();

            // Create a new jobs
            var connectionJob = new ServerUpdateConnectionsJob
            {
                driver = m_Driver,
                connections = m_Connections,
                connectionsEventsQueue = m_ConnectionsEventsQueue.AsParallelWriter()
            };

            var serverUpdateJob = new ServerUpdateJob
            {
                driver = m_Driver.ToConcurrent(),
                connections = m_Connections.AsDeferredJobArray(),
                connectionsEventsQueue = m_ConnectionsEventsQueue.AsParallelWriter()
            };

            // Schedule jobs
            m_ServerJobHandle = m_Driver.ScheduleUpdate();
            m_ServerJobHandle = connectionJob.Schedule(m_ServerJobHandle);
            m_ServerJobHandle = serverUpdateJob.Schedule(m_Connections, 1, m_ServerJobHandle);
        }

		/// <summary>
		/// Stop a running server.
		/// </summary>
		public void Stop()
        {
            UtpLog.Info("Stopping server");

            // Because of the way that UTP works we need to delay our calls to dispose the driver.
            // This allows all clients that are connected to receive a NetworkEvent.Type.Disconnect event
            // TODO: Determine a less hacky way to accomplish this
            m_CoroutineRunner.StartCoroutine(DisposeAfterWait());
		}

        private IEnumerator DisposeAfterWait()
		{
			yield return new WaitForSeconds(0.25f);

			m_ServerJobHandle.Complete();

            m_ConnectionsEventsQueue.Dispose();
			m_Connections.Dispose();
			m_Driver.Dispose();
			m_Driver = default(NetworkDriver);
		}

        /// <summary>
        /// Disconnect and remove a connection via it's ID.
        /// </summary>
        /// <param name="connectionId">The ID of the connection to disconnect.</param>
        public void Disconnect(int connectionId)
        {
            m_ServerJobHandle.Complete();

            Unity.Networking.Transport.NetworkConnection connection = FindConnection(connectionId);
            if (connection.GetHashCode() == connectionId)
            {
                UtpLog.Info("Disconnecting connection with ID: " + connectionId);
                connection.Disconnect(m_Driver);
                OnDisconnected.Invoke(connectionId);
            }
            else
            {
                UtpLog.Warning("connection not found: " + connectionId);
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
            m_ServerJobHandle.Complete();

            System.Type stageType = channelId == Channels.Reliable ? typeof(ReliableSequencedPipelineStage) : typeof(UnreliableSequencedPipelineStage);
            NetworkPipeline pipeline = channelId == Channels.Reliable ? m_ReliablePipeline : m_UnreliablePipeline;

            Unity.Networking.Transport.NetworkConnection connection = FindConnection(connectionId);
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
            else
            {
                UtpLog.Warning("connection not found: " + connectionId);
            }
        }

        /// <summary>
        /// Look up a client's address via it's ID.
        /// </summary>
        /// <param name="connectionId">The ID of the connection.</param>
        /// <returns>The client address.</returns>
        public string GetClientAddress(int connectionId)
        {
            Unity.Networking.Transport.NetworkConnection connection = FindConnection(connectionId);
            if (connection.GetHashCode() == connectionId)
            {
                NetworkEndPoint endpoint = m_Driver.RemoteEndPoint(connection);
                return endpoint.Address;
            }
            else
            {
                UtpLog.Warning("connection not found: " + connectionId);
                return "";
            }
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
            if (!IsActive() || !NetworkServer.active)
                return;

            UtpConnectionEvent connectionEvent;
            while (m_ConnectionsEventsQueue.TryDequeue(out connectionEvent))
            {
                Unity.Networking.Transport.NetworkConnection connection = FindConnection(connectionEvent.connectionId);
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
                        UtpLog.Warning("invalid connection event: " + connectionEvent.eventType);
                    }
                }
                else
                {
                    UtpLog.Warning("connection not found: " + connectionEvent.connectionId);
                }
            }
        }

        /// <summary>
        /// Processes connection events from the queue.
        /// </summary>
        /// <param name="connectionId">The ID of the connection to find.</param>
        /// <returns>The connection if found in the list, a default connection otherwise.</returns>
        public Unity.Networking.Transport.NetworkConnection FindConnection(int connectionId)
        {
            foreach (Unity.Networking.Transport.NetworkConnection connection in m_Connections)
            {
                if(connection.GetHashCode() == connectionId)
                {
                    return connection;
                }
            }
            return default(Unity.Networking.Transport.NetworkConnection);
        }
    }
}
