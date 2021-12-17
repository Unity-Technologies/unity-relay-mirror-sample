using System;
using System.Collections.Generic;

using Mirror;

using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Utp
{
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
		/// Used to bind, listen, and send data to connections.
		/// </summary>
		private NetworkDriver m_Driver;

		/// <summary>
		/// Client connections to this server.
		/// </summary>
		private Dictionary<int, UtpServerConnection> m_Connections = new Dictionary<int, UtpServerConnection>();

		/// <summary>
		/// A pipeline on the driver that is sequenced, and ensures messages are delivered.
		/// </summary>
		private NetworkPipeline m_ReliablePipeline;

		/// <summary>
		/// A pipeline on the driver that is sequenced, but does not ensure messages are delivered.
		/// </summary>
		private NetworkPipeline m_UnreliablePipeline;

		private bool m_IsRelayServerConnected;

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
		public void Start(ushort port, bool useRelay = false, Allocation allocation = null)
		{
			if (IsActive())
			{
				UtpLog.Warning("Server already active");
				return;
			}

			NetworkEndPoint endpoint = NetworkEndPoint.AnyIpv4;
			if (useRelay)
			{
				RelayServerData relayServerData = RelayUtils.HostRelayData(allocation, "udp");
				RelayNetworkParameter relayNetworkParameter = new RelayNetworkParameter { ServerData = relayServerData };

				m_Driver = NetworkDriver.Create(new INetworkParameter[] { relayNetworkParameter }); // TODO: use Create(NetworkSettings) instead
			}
			else
			{
				m_Driver = NetworkDriver.Create();
				endpoint.Port = port;
			}

			m_ReliablePipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
			m_UnreliablePipeline = m_Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));

			if (m_Driver.Bind(endpoint) != 0) // TODO: do we need to wait for bind to finish?
			{
				UtpLog.Error("Failed to bind to port: " + endpoint.Port);
			}
			else
			{
				if (m_Driver.Listen() != 0)
				{
					UtpLog.Error("Server failed to listen");
				}
				else if (useRelay)
				{
					m_IsRelayServerConnected = true;
				}
			}

			UtpLog.Info(useRelay ? ("Server started") : ("Server started on port: " + endpoint.Port));
		}

		/// <summary>
		/// Tick the server, pumping it's driver and managing it's connections.
		/// </summary>
		public void Tick()
		{
			if (!IsActive())
				return;

			// Pump the driver
			m_Driver.ScheduleUpdate().Complete();

			// Clean up connections
			{
				HashSet<int> connectionsToRemove = new HashSet<int>();
				foreach (KeyValuePair<int, UtpServerConnection> connection in m_Connections)
				{
					if (!connection.Value.networkConnection.IsCreated)
					{
						connectionsToRemove.Add(connection.Key);
					}
					else if (connection.Value.IsTimedOut())
					{
						UtpLog.Info("Client has timed out. Connection ID: " + connection.Key);
						Disconnect(connection.Key);
						connectionsToRemove.Add(connection.Key);
					}
				}

				foreach (int connectionId in connectionsToRemove)
				{
					UtpLog.Info("Removing connection with ID: " + connectionId);
					m_Connections.Remove(connectionId);
				}
				connectionsToRemove.Clear();
			}

			// Accept new connections
			{
				Unity.Networking.Transport.NetworkConnection networkConnection;
				while ((networkConnection = m_Driver.Accept()) != default(Unity.Networking.Transport.NetworkConnection))
				{
					UtpLog.Info("Adding connection with ID: " + networkConnection.GetHashCode());
					m_Connections.Add(networkConnection.GetHashCode(), new UtpServerConnection(networkConnection, OnReceivedData, OnDisconnected));
					OnConnected.Invoke(networkConnection.GetHashCode());
				}
			}

			// Query incoming events for all connections
			{
				foreach (KeyValuePair<int, UtpServerConnection> connection in m_Connections)
				{
					if (!connection.Value.networkConnection.IsCreated)
						continue;


					// Process all incoming events for this connection
					connection.Value.ProcessIncomingEvents(m_Driver);
				}
			}
		}

		/// <summary>
		/// Stop a running server.
		/// </summary>
		public void Stop()
		{
			UtpLog.Info("Stopping server");

			m_Driver.Dispose();
			m_Driver = default(NetworkDriver);
		}

		/// <summary>
		/// Disconnect and remove a connection via it's ID.
		/// </summary>
		/// <param name="connectionId">The ID of the connection to disconnect.</param>
		public void Disconnect(int connectionId)
		{
			if (m_Connections.TryGetValue(connectionId, out UtpServerConnection connection))
			{
				UtpLog.Info("Disconnecting connection with ID: " + connectionId);
				connection.networkConnection.Disconnect(m_Driver);
				OnDisconnected(connectionId);
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
			if (m_Connections.TryGetValue(connectionId, out UtpServerConnection connection))
			{
				connection.Send(m_Driver,
					channelId == Channels.Reliable ? m_ReliablePipeline : m_UnreliablePipeline,
					channelId == Channels.Reliable ? typeof(ReliableSequencedPipelineStage) : typeof(UnreliableSequencedPipelineStage),
					segment);
			}
		}

		/// <summary>
		/// Look up a client's address via it's ID.
		/// </summary>
		/// <param name="connectionId">The ID of the connection.</param>
		/// <returns>The client address.</returns>
		public string GetClientAddress(int connectionId)
		{
			UtpLog.Verbose("Looking for client address with connection ID: " + connectionId);

			if (m_Connections.TryGetValue(connectionId, out UtpServerConnection connection))
			{
				NetworkEndPoint endpoint = m_Driver.RemoteEndPoint(connection.networkConnection);
				return endpoint.Address;
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
	}
}
