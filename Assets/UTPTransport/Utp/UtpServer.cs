using Mirror;
using System;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

namespace Utp
{

    #region Jobs

    /// <summary>
    /// Job used to update connections. 
    /// </summary>
    [BurstCompile]
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
			for (int i = 0; i < connections.Length; i++)
			{
				//If a connection is no longer established...
				if(connections[i].GetState(driver) == Unity.Networking.Transport.NetworkConnection.State.Disconnected)
				{
					//Remove connection and then decrement to continue search
					connections.RemoveAtSwapBack(i--);
				}
			}

			// Accept new connections
			Unity.Networking.Transport.NetworkConnection networkConnection;
			while ((networkConnection = driver.Accept()) != default(Unity.Networking.Transport.NetworkConnection))
			{
				UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
				connectionEvent.eventType = (byte)UtpConnectionEventType.OnConnected;
				connectionEvent.connectionId = networkConnection.GetHashCode();
				connectionsEventsQueue.Enqueue(connectionEvent);

				connections.Add(networkConnection);
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
	[BurstCompile]
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
					UtpConnectionEvent connectionEvent = new UtpConnectionEvent();
					connectionEvent.eventType = (byte)UtpConnectionEventType.OnDisconnected;
					connectionEvent.connectionId = connections[index].GetHashCode();
					connectionsEventsQueue.Enqueue(connectionEvent);
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
	struct ServerSendJob : IJob
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
			
			//If Acquire was success
			if (writeStatus == (int)Unity.Networking.Transport.Error.StatusCode.Success)
			{
				writer.WriteBytes(data);
				driver.EndSend(writer);
			}
		}
	}

	#endregion

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
		/// Temporary storage for connection events that occur on job threads so they may be dequeued on the main thread.
		/// </summary>
		private NativeQueue<UtpConnectionEvent> connectionsEventsQueue;

		/// <summary>
		/// Used to bind, listen, and send data to connections.
		/// </summary>
		private NetworkDriver driver;

		/// <summary>
		/// Client connections to this server.
		/// </summary>
		private NativeList<Unity.Networking.Transport.NetworkConnection> connections;

		/// <summary>
		/// Job handle to schedule server jobs.
		/// </summary>
		private JobHandle serverJobHandle;

		/// <summary>
		/// A pipeline on the driver that is sequenced, and ensures messages are delivered.
		/// </summary>
		private NetworkPipeline reliablePipeline;

		/// <summary>
		/// A pipeline on the driver that is sequenced, but does not ensure messages are delivered.
		/// </summary>
		private NetworkPipeline unreliablePipeline;

		/// <summary>
		/// Timeout(ms) to be set on drivers.
		/// </summary>
		private int timeout;

		public UtpServer(Action<int> OnConnected,
			Action<int, ArraySegment<byte>> OnReceivedData,
			Action<int> OnDisconnected,
			int timeout)
		{
			this.OnConnected = OnConnected;
			this.OnReceivedData = OnReceivedData;
			this.OnDisconnected = OnDisconnected;
			this.timeout = timeout;
		}

		/// <summary>
		/// Initialize the server. Currently only supports IPV4.
		/// </summary>
		/// <param name="port">The port to listen for connections on.</param>
		/// <param name="useRelay">Whether or not to use start a server using Unity's Relay Service.</param>
		/// <param name="allocation">The Relay allocation, if using Relay.</param>
		public void Start(ushort port, bool useRelay = false, Allocation allocation = null)
		{
			if (IsActive())
			{
				UtpLog.Warning("Server already active");
				return;
			}

			var settings = new NetworkSettings();
			settings.WithNetworkConfigParameters(disconnectTimeoutMS: timeout);

			NetworkEndPoint endpoint = NetworkEndPoint.AnyIpv4;
			endpoint.Port = port;

			if (useRelay)
			{
				RelayServerData relayServerData = RelayUtils.HostRelayData(allocation, "udp");
				RelayNetworkParameter relayNetworkParameter = new RelayNetworkParameter { ServerData = relayServerData };
				NetworkSettings networkSettings = new NetworkSettings();
				RelayParameterExtensions.WithRelayParameters(ref networkSettings, ref relayServerData);
				driver = NetworkDriver.Create(networkSettings);
			}
			else
			{
				NetworkSettings networkSettings = new NetworkSettings();
				driver = NetworkDriver.Create(networkSettings);
				endpoint.Port = port;
			}

			connections = new NativeList<Unity.Networking.Transport.NetworkConnection>(16, Allocator.Persistent);
			connectionsEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);
			reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
			unreliablePipeline = driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));

			if (driver.Bind(endpoint) != 0)
			{
				UtpLog.Error($"Failed to bind to port: {endpoint.Port}");
			}
			else
			{
				if (driver.Listen() != 0)
				{
					UtpLog.Error("Server failed to listen");
				}
			}

			UtpLog.Info(useRelay ? ("Server started") : ($"Server started on port: {endpoint.Port}"));
		}

		/// <summary>
		/// Tick the server, creating the server jobs and scheduling them. Processes events created by the jobs.
		/// </summary>
		public void Tick()
		{
			if (!IsActive())
				return;

			// First complete the job that was initialized in the previous frame
			serverJobHandle.Complete();

			// Trigger Mirror callbacks for events that resulted in the last jobs work
			ProcessIncomingEvents();

			// Create a new jobs
			var connectionJob = new ServerUpdateConnectionsJob
			{
				driver = driver,
				connections = connections,
				connectionsEventsQueue = connectionsEventsQueue.AsParallelWriter()
			};

			var serverUpdateJob = new ServerUpdateJob
			{
				driver = driver.ToConcurrent(),
				connections = connections.AsDeferredJobArray(),
				connectionsEventsQueue = connectionsEventsQueue.AsParallelWriter()
			};

			// Schedule jobs
			// We are explicitly scheduling ServerUpdateJob before ServerUpdateConnectionsJob so that disconnect events are enqueued before the corresponding NetworkConnection is removed.
			serverJobHandle = driver.ScheduleUpdate();
			serverJobHandle = serverUpdateJob.Schedule(connections, 1, serverJobHandle);
			serverJobHandle = connectionJob.Schedule(serverJobHandle);
		}

		/// <summary>
		/// Stop a running server.
		/// </summary>
		public void Stop()
		{
			UtpLog.Info("Stopping server");

			serverJobHandle.Complete();
			connectionsEventsQueue.Dispose();
			connections.Dispose();
			driver.Dispose();
			driver = default(NetworkDriver);
		}

		/// <summary>
		/// Disconnect and remove a connection via it's ID.
		/// </summary>
		/// <param name="connectionId">The ID of the connection to disconnect.</param>
		public void Disconnect(int connectionId)
		{
			serverJobHandle.Complete();

			Unity.Networking.Transport.NetworkConnection connection = FindConnection(connectionId);
			if (connection.GetHashCode() == connectionId)
			{
				UtpLog.Info($"Disconnecting connection with ID: {connectionId}");
				connection.Disconnect(driver);

				// When disconnecting, we need to ensure the driver has the opportunity to send a disconnect event to the client
				driver.ScheduleUpdate().Complete();

				OnDisconnected.Invoke(connectionId);
			}
			else
			{
				UtpLog.Warning($"Connection not found: {connectionId}");
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
			serverJobHandle.Complete();

			Unity.Networking.Transport.NetworkConnection connection = FindConnection(connectionId);

			if (connection.GetHashCode() == connectionId)
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
				serverJobHandle = job.Schedule(serverJobHandle);
			}
			else
			{
				UtpLog.Warning($"Connection not found: {connectionId}");
			}
		}

		/// <summary>
		/// Look up a client's address via it's ID. If using Relay, this will always return the address of the Relay server.
		/// </summary>
		/// <param name="connectionId">The ID of the connection.</param>
		/// <returns>The client address, or Relay server if using Relay.</returns>
		public string GetClientAddress(int connectionId)
		{
			Unity.Networking.Transport.NetworkConnection connection = FindConnection(connectionId);
			if (connection.GetHashCode() == connectionId)
			{
				NetworkEndPoint endpoint = driver.RemoteEndPoint(connection);
				return endpoint.Address;
			}
			else
			{
				UtpLog.Warning($"Connection not found: {connectionId}");
				return String.Empty;
			}
		}

		/// <summary>
		/// Determine whether the server is running or not.
		/// </summary>
		/// <returns>True if running, false otherwise.</returns>
		public bool IsActive()
		{
			return !Equals(driver, default(NetworkDriver));
		}

		/// <summary>
		/// Processes connection events from the queue.
		/// </summary>
		public void ProcessIncomingEvents()
		{
			if (!IsActive() || !NetworkServer.active)
				return;

			UtpConnectionEvent connectionEvent;
			while (connectionsEventsQueue.TryDequeue(out connectionEvent))
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
					UtpLog.Warning($"Invalid connection event: {connectionEvent.eventType}");
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
			foreach (Unity.Networking.Transport.NetworkConnection connection in connections)
			{
				if (connection.GetHashCode() == connectionId)
				{
					return connection;
				}
			}
			return default(Unity.Networking.Transport.NetworkConnection);
		}
	}
}
