using Mirror;

using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;
using Unity.Services.Relay.Models;

namespace Utp
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

		// Relay toggle
		public bool useRelay;

		// Relay Manager
		RelayManager relayManager;

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

			relayManager = gameObject.AddComponent<RelayManager>();

			UtpLog.Info("UTPTransport initialized!");
		}

		public override bool Available()
		{
			return Application.platform != RuntimePlatform.WebGLPlayer;
		}

		// Client
		public override void ClientConnect(string address)
		{
			if (useRelay)
			{
				// We entirely ignore the address that is passed when utilizing Relay
				// The data we need to connect is embedded in the relayManager's JoinAllocation
				client.RelayConnect(relayManager.joinAllocation);
			}
			else
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
		}

		public override bool ClientConnected() => client.IsConnected();
		public override void ClientDisconnect() => client.Disconnect();
		public override void ClientSend(ArraySegment<byte> segment, int channelId) => client.Send(segment, channelId);

		public override void ClientEarlyUpdate()
		{
			if (enabled) client.Tick();
		}

		// Relay Client (Only used if Relay is enabled)
		public void ConfigureClientWithJoinCode(string joinCode, Action<string> callback)
		{
			relayManager.GetAllocationFromJoinCode(joinCode, callback);
		}

		// Server
		public override bool ServerActive() => server.IsActive();
		public override void ServerStart()
		{
			server.Start(Port, useRelay, relayManager.serverAllocation);
		}

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

		// Relay Server (Only used if Relay is enabled)
		public void GetRelayRegions(Action<List<Region>> callback)
		{
			relayManager.GetRelayRegions(callback);
		}

		public void AllocateRelayServer(int maxPlayers, string regionId, Action<string, string> callback)
		{
			relayManager.OnRelayServerAllocated = callback;
			relayManager.AllocateRelayServer(maxPlayers, regionId);
		}

		public override int GetMaxPacketSize(int channelId = Channels.Reliable)
		{
			if(client != null)
            {
				return NetworkParameterConstants.MTU - client.GetMaxHeaderSize();
			} 
			else
            {
				return NetworkParameterConstants.MTU;
            }
		}

		public override void Shutdown() 
		{
			if (client.IsConnected()) client.Disconnect();
			if (server.IsActive()) server.Stop();
		}

		public override string ToString() => "UTP";
	}
}