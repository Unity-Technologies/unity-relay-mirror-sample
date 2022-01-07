using Mirror;

using System;

using Unity.Networking.Transport;
using UnityEngine;

namespace UtpTransport
{
    /// <summary>
    /// Component that implements Mirror's Transport class, utilizing the Unity Transport Package (UTP).
    /// </summary>
    [DisallowMultipleComponent]
	public class UtpTransport : Transport
	{
		// Scheme used by this transport
		public const string Scheme = "udp";

		// Common
		[Header("Transport Configuration")]
		public ushort Port = 7777;
		[Header("Debugging")]
		public LogLevel LoggerLevel = LogLevel.Info;
		[Header("Timeout in MS")]
		public int TimeoutMS = 1000;

		// Server & Client
		UtpServer server;
		UtpClient client;

		private void Awake()
		{
			if (LoggerLevel < LogLevel.Verbose) UtpLog.Verbose = _ => {};
			if (LoggerLevel < LogLevel.Info) UtpLog.Info = _ => {};
			if (LoggerLevel < LogLevel.Warning) UtpLog.Warning = _ => {};
			if (LoggerLevel < LogLevel.Error) UtpLog.Error = _ => {};

			server = new UtpServer(
				(connectionId) => OnServerConnected.Invoke(connectionId),
				(connectionId, message) => OnServerDataReceived.Invoke(connectionId, message, Channels.Reliable),
				(connectionId) => OnServerDisconnected.Invoke(connectionId),
				TimeoutMS);
			client = new UtpClient(
				() => OnClientConnected.Invoke(),
				(message) => OnClientDataReceived.Invoke(message, Channels.Reliable),
				() => OnClientDisconnected.Invoke(),
				TimeoutMS);

			UtpLog.Info("UTPTransport initialized!");
		}

		public override bool Available()
		{
			// TODO: What determines if this is actually available?
			return true;
		}

		// Client
		public override void ClientConnect(string address)
		{
			if (address.Contains(":"))
			{
				string[] hostAndPort = address.Split(':');
				client.Connect(hostAndPort[0], Convert.ToUInt16(hostAndPort[1]));
			}
			else
			{
				client.Connect(address, Port); // fallback to default port
			}
		}

		public override bool ClientConnected() => client.IsConnected();
		public override void ClientDisconnect() => client.Disconnect();
		public override void ClientSend(ArraySegment<byte> segment, int channelId) => client.Send(segment, channelId);

		public override void ClientEarlyUpdate()
		{
			if (enabled) client.Tick();
		}

		// TODO: implement OnEnable/OnDisable

		// Server
		public override bool ServerActive() => server.IsActive();
		public override void ServerStart() => server.Start(Port);
		public override void ServerStop() => server.Stop();
		public override string ServerGetClientAddress(int connectionId) => server.GetClientAddress(connectionId);
		public override void ServerDisconnect(int connectionId) => server.Disconnect(connectionId);
		public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId) => server.Send(connectionId, segment, channelId);

		public override void ServerEarlyUpdate()
		{
			if (enabled) server.Tick();
		}

		public override Uri ServerUri()
		{
			UriBuilder builder = new UriBuilder();
			builder.Scheme = Scheme;
			builder.Port = Port;

			return builder.Uri;
		}

		// Common
		public override int GetMaxPacketSize(int channelId = Channels.Reliable)
		{
			// TODO: better definitions for max packet size. Consider the channel type and the need for protocol and pipeline overhead.
			// See: NetworkDriver.cs / BeginSend()
			return NetworkParameterConstants.MTU;
		}

		public override void Shutdown() { }

		public override string ToString() => "UTP";
	}
}