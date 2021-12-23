using System;

using Mirror;

using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Utp
{
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
		/// Used alongside a connection to connect, send, and receive data from a listen server.
		/// </summary>
		private NetworkDriver m_Driver;

		/// <summary>
		/// Used alongside a driver to connect, send, and receive data from a listen server.
		/// </summary>
		private UtpClientConnection m_Connection;

		/// <summary>
		/// A pipeline on the driver that is sequenced, and ensures messages are delivered.
		/// </summary>
		private NetworkPipeline m_ReliablePipeline;

		/// <summary>
		/// A pipeline on the driver that is sequenced, but does not ensure messages are delivered.
		/// </summary>
		private NetworkPipeline m_UnreliablePipeline;

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

			m_Driver = NetworkDriver.Create();
			m_ReliablePipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
			m_UnreliablePipeline = m_Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
			m_Connection = new UtpClientConnection(OnConnected, OnReceivedData, OnDisconnected);

			if (host == "localhost")
			{
				host = "127.0.0.1";
			}

			NetworkEndPoint endpoint = NetworkEndPoint.Parse(host, port); // TODO: also support IPV6
			m_Connection.Connect(m_Driver, endpoint);

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
			m_Connection = new UtpClientConnection(OnConnected, OnReceivedData, OnDisconnected);

			m_Connection.Connect(m_Driver, relayNetworkParameter.ServerData.Endpoint);

			UtpLog.Info("Client connecting to server at: " + relayNetworkParameter.ServerData.Endpoint.Address);
		}

		/// <summary>
		/// Whether or not the client is connected to a server.
		/// </summary>
		/// <returns>True if connected to a server, false otherwise.</returns>
		public bool IsConnected()
		{
			return DriverActive() &&
				m_Connection.networkConnection.GetState(m_Driver) == Unity.Networking.Transport.NetworkConnection.State.Connected;
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
			m_Driver.Dispose();
			m_Driver = default(NetworkDriver);
		}

		/// <summary>
		/// Tick the client, pumping it's driver and managing it's connection.
		/// </summary>
		public void Tick()
		{
			if (!DriverActive())
				return;

			// Pump the driver
			m_Driver.ScheduleUpdate().Complete();

			// Exit if the connection is not ready
			if (!m_Connection.networkConnection.IsCreated)
				return;

			// Process all incoming events for this connection
			m_Connection.ProcessIncomingEvents(m_Driver);
		}

		/// <summary>
		/// Send data to the listen server over a particular channel.
		/// </summary>
		/// <param name="segment">The data to send.</param>
		/// <param name="channelId">The 'Mirror.Channels' channel to send the data over.</param>
		public void Send(ArraySegment<byte> segment, int channelId)
		{
			if (!DriverActive() || !m_Connection.networkConnection.IsCreated)
				return;

			m_Connection.Send(m_Driver,
				channelId == Channels.Reliable ? m_ReliablePipeline : m_UnreliablePipeline,
				channelId == Channels.Reliable ? typeof(ReliableSequencedPipelineStage) : typeof(UnreliableSequencedPipelineStage),
				segment);
		}
	}

}