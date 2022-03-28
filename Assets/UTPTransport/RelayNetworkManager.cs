using System;
using System.Collections.Generic;

using Mirror;

using UnityEngine;
using Unity.Services.Relay.Models;

namespace Utp
{
    public class RelayNetworkManager : NetworkManager
    {
		private UtpTransport utpTransport;

		/// <summary>
		/// Server's join code if using Relay.
		/// </summary>
		public string relayJoinCode = "";

		/// <summary>
		/// An instance of the UTP logger.
		/// </summary>
		public UtpLog logger;

		public RelayNetworkManager()
        {
			logger = new UtpLog("[RelayNetworkManager] ");
        }

		public override void Awake()
		{
			base.Awake();

			utpTransport = GetComponent<UtpTransport>();

			string[] args = System.Environment.GetCommandLineArgs();
			for (int key = 0; key < args.Length; key++)
			{
				if (args[key] == "-port")
				{
					if (key + 1 < args.Length)
					{
						string value = args[key + 1];

						try
						{
							utpTransport.Port = ushort.Parse(value);
						}
						catch
						{
							logger.Warning($"Unable to parse {value} into transport Port");
						}
					}
				}
			}
		}

        /// <summary>
        /// Get the port the server is listening on.
        /// </summary>
        /// <returns>The port.</returns>
        public ushort GetPort()
		{
			return utpTransport.Port;
		}

		/// <summary>
		/// Get whether Relay is enabled or not.
		/// </summary>
		/// <returns>True if enabled, false otherwise.</returns>
		public bool IsRelayEnabled()
		{
			return utpTransport.useRelay;
		}

		/// <summary>
		/// Ensures Relay is disabled. Starts the server, listening for incoming connections.
		/// </summary>
		public void StartStandardServer()
		{
			utpTransport.useRelay = false;
			StartServer();
		}

		/// <summary>
		/// Ensures Relay is disabled. Starts a network "host" - a server and client in the same application
		/// </summary>
		public void StartStandardHost()
		{
			utpTransport.useRelay = false;
			StartHost();
		}

		/// <summary>
		/// Gets available Relay regions.
		/// </summary>
		/// 
		public void GetRelayRegions(Action<List<Region>> callback)
		{
			utpTransport.GetRelayRegions(callback);
		}

		/// <summary>
		/// Ensures Relay is enabled. Starts a network "host" - a server and client in the same application
		/// </summary>
		public void StartRelayHost(int maxPlayers, string regionId = null)
		{
			utpTransport.useRelay = true;
			utpTransport.AllocateRelayServer(maxPlayers, regionId, (string joinCode, string error) =>
			{
				if (error != null)
				{
					Debug.LogWarning("Something went wrong allocating Relay Server. Error: " + error);
					return;
				}

				relayJoinCode = joinCode;
				StartHost();
			});
		}

		/// <summary>
		/// Ensures Relay is disabled. Starts the client, connects it to the server with networkAddress.
		/// </summary>
		public void JoinStandardServer()
		{
			utpTransport.useRelay = false;
			StartClient();
		}

		/// <summary>
		/// Ensures Relay is enabled. Starts the client, connects to the server with the relayJoinCode.
		/// </summary>
		public void JoinRelayServer()
		{
			utpTransport.useRelay = true;
			utpTransport.ConfigureClientWithJoinCode(relayJoinCode, (string error) =>
			{
				if (error != null)
				{
					Debug.LogWarning("Something went wrong joining Relay server with code: " + relayJoinCode + ", Error: " + error);
					return;
				}

				StartClient();
			});
		}

		/// <summary>
		/// Enables logging for this module.
		/// </summary>
		/// <param name="logLevel">The log level to set this logger to.</param>
		public void EnableLogging(LogLevel logLevel = LogLevel.Verbose)
		{
			logger.SetLogLevel(logLevel);
		}

		/// <summary>
		/// Disables logging for this module.
		/// </summary>
		public void DisableLogging()
		{
			logger.SetLogLevel(LogLevel.Off);
		}
	}
}