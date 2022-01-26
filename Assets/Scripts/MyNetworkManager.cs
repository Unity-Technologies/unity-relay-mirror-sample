using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Unity.Helpers.ServerQuery.ServerQuery;
using Unity.Helpers.ServerQuery.Data;
using Unity.Helpers.ServerQuery;
using Unity.Helpers.ServerQuery.Protocols.SQP.Collections;
using Unity.Helpers.ServerQuery.Protocols.A2S.Collections;
using Unity.Helpers.ServerQuery.Protocols.TF2E.Collections;
using Unity.Services.Authentication;
using Unity.Services.Core;

using Utp;

namespace Network 
{
    public class MyNetworkManager : RelayNetworkManager
    {
        /// <summary>
        /// The local player object that spawns in.
        /// </summary>
        public Player localPlayer;
        private ServerQueryManager m_ServerQueryManager;
        private string m_SessionId = "";
        private string m_Username;
        private string m_UserId;
        private bool m_IsDedicatedServer;
        private ServerQueryServer.Protocol m_Protocol;
        private string m_Version = "001";
        private ushort m_QueryPort = 0;

        /// <summary>
        /// Flag to determine if the user is logged into the backend.
        /// </summary>
        public bool isLoggedIn = false;

        /// <summary>
        /// List of players currently connected to the server.
        /// </summary>
        private List<Player> m_Players;

        /// <summary>
        /// Player Index, used to assign player ID for server query. Increments each time player is added.
        /// </summary>
        private byte m_PlayerIndex = 1;

        public override void Awake()
        {
            base.Awake();
            m_Protocol = ServerQueryServer.Protocol.SQP;
            m_IsDedicatedServer = false;
            m_Players = new List<Player>();

            m_Username = SystemInfo.deviceName;

            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {

                if (args[i] == "-queryport")
                {
                    if (i + 1 < args.Length)
                    {
                        try
                        {
                            m_QueryPort = ushort.Parse(args[i + 1]);
                            Debug.Log($"found query port {m_QueryPort}");
                        }
                        catch
                        {
                            Debug.Log($"unable to parse {args[i + 1]} into ushort for query port");
                        }
                    }
                }
                else if (args[i] == "-queryprotocol")
                {
                    if (i + 1 < args.Length)
                    {
                        if (args[i + 1] == "sqp")
                        {
                            m_Protocol = ServerQueryServer.Protocol.SQP;
                        }
                        else if (args[i + 1] == "a2s")
                        {
                            m_Protocol = ServerQueryServer.Protocol.A2S;
                        }
                        else if (args[i + 1] == "tf2e")
                        {
                            m_Protocol = ServerQueryServer.Protocol.TF2E;
                        }
                        else
                        {
                            Debug.Log("incompatible query type found, defaulting to SQP");
                            continue;
                        }
                        Debug.Log($"found query protocol: {args[i + 1]}");
                    }
                }
                else if (args[i] == "-version")
                {
                    if (i + 1 < args.Length)
                    {
                        m_Version = args[i + 1];
                        Debug.Log($"found game version {m_Version}");
                    }
                }
                else if (args[i] == "-server")
                {
                    m_IsDedicatedServer = true;
                    Debug.Log($"starting as dedicated server");
                }
            }
        }

        public async void UnityLogin()
		{
			try
			{
				await UnityServices.InitializeAsync();
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Logged into Unity, player ID: " + AuthenticationService.Instance.PlayerId);
                isLoggedIn = true;
            }
			catch (Exception e)
			{
                isLoggedIn = false;
                Debug.Log(e);
			}
		}

        private void Update()
        {
            if (NetworkManager.singleton.isNetworkActive)
            {
                if (localPlayer == null)
                {
                    FindLocalPlayer();
                }
            }
            else
            {
                localPlayer = null;
                m_Players.Clear();
            }

            if (m_ServerQueryManager)
            {
                m_ServerQueryManager.UpdateQueryData(UpdateQueryData());
            }
        }

        /// <summary>
        /// Creates an updated QueryData object for ServerQuery.
        /// </summary>
        /// <returns> Updated QueryData object for the server.</returns>
        private QueryData UpdateQueryData()
        {
            QueryData data = new QueryData();

            if (m_Protocol == ServerQueryServer.Protocol.SQP)
            {
                // SQP Server Data
                data.SQPServerInfo.currentPlayers = m_Players.Count;
                data.SQPServerInfo.maxPlayers = 8;
                data.SQPServerInfo.serverName = "Default Server Name";
                data.SQPServerInfo.gameType = "Default Game Type";
                data.SQPServerInfo.buildID = "Default Build ID";
                data.SQPServerInfo.map = "Default Map Name";
                data.SQPServerInfo.gamePort = GetPort();

                // SQP Rule Data
                SQPServerRule rule = new SQPServerRule();
                rule.key = "TestRule";
                rule.type = (byte)SQPDynamicType.String;
                rule.valueString = "TestValue";

                data.SQPServerRules.rules.Add(rule);

                // SQP Player Data
                data.SQPPlayerInfo.playerCount = (ushort)m_Players.Count;
                data.SQPPlayerInfo.fieldCount = 1;

                // SQP Team Data
                data.SQPTeamInfo.teamCount = 2;
                data.SQPTeamInfo.fieldCount = 1;

                SQPFieldKeyValue teamFieldOne = new SQPFieldKeyValue();
                teamFieldOne.key = "Score";
                teamFieldOne.type = (byte)SQPDynamicType.Uint;
                teamFieldOne.valueUInt = 23;

                SQPFieldKeyValue teamFieldTwo = new SQPFieldKeyValue();
                teamFieldTwo.key = "Score";
                teamFieldTwo.type = (byte)SQPDynamicType.Uint;
                teamFieldTwo.valueUInt = 11;

                List<SQPFieldKeyValue> teamFieldsOne = new List<SQPFieldKeyValue>();
                teamFieldsOne.Add(teamFieldOne);
                List<SQPFieldKeyValue> teamFieldsTwo = new List<SQPFieldKeyValue>();
                teamFieldsTwo.Add(teamFieldTwo);

                SQPFieldContainer teamOne = new SQPFieldContainer();
                teamOne.fields = teamFieldsOne;
                SQPFieldContainer teamTwo = new SQPFieldContainer();
                teamTwo.fields = teamFieldsTwo;

                data.SQPTeamInfo.teams.Add(teamOne);
                data.SQPTeamInfo.teams.Add(teamTwo);

                foreach (Player player in m_Players)
                {
                    SQPFieldKeyValue playerField = new SQPFieldKeyValue();
                    playerField.key = "PlayerName";
                    playerField.type = (byte)SQPDynamicType.String;
                    playerField.valueString = player.username;

                    List<SQPFieldKeyValue> fields = new List<SQPFieldKeyValue>();
                    fields.Add(playerField);

                    SQPFieldContainer playerOne = new SQPFieldContainer();
                    playerOne.fields = fields;

                    data.SQPPlayerInfo.players.Add(playerOne);
                }
            }

            if (m_Protocol == ServerQueryServer.Protocol.A2S)
            {
                // A2S Server Data
                data.A2SServerInfo.protocol = 0;
                data.A2SServerInfo.serverName = "Default Server Name";
                data.A2SServerInfo.serverMap = "Default Scene";
                data.A2SServerInfo.folder = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
                data.A2SServerInfo.gameName = "Unity Mirror Sample";
                data.A2SServerInfo.steamID = 12345;
                data.A2SServerInfo.playerCount = (byte)m_Players.Count;
                data.A2SServerInfo.maxPlayers = 8;
                data.A2SServerInfo.botCount = 0;
                data.A2SServerInfo.serverType = (byte)'d';
                data.A2SServerInfo.visibility = 0;
                data.A2SServerInfo.valveAntiCheat = 0;
                data.A2SServerInfo.version = m_Version;
                data.A2SServerInfo.extraDataFlag = 0;

                // A2S rules data
                A2SRulesResponsePacketKeyValue rule = new A2SRulesResponsePacketKeyValue();
                rule.ruleName = "TestRule";
                rule.ruleValue = "TestValue";

                data.A2SServerRules.numRules = 1;
                data.A2SServerRules.rules.Add(rule);

                // A2S Player data

                data.A2SPlayerInfo.numPlayers = (byte)m_Players.Count;

                foreach (Player player in m_Players)
                {
                    A2SPlayerResponsePacketPlayer playerPacket = new A2SPlayerResponsePacketPlayer();
                    playerPacket.index = m_PlayerIndex;
                    playerPacket.playerName = player.username;
                    playerPacket.score = 0;
                    playerPacket.duration = 0;

                    data.A2SPlayerInfo.players.Add(playerPacket);

                    m_PlayerIndex++;
                }
            }

            if (m_Protocol == ServerQueryServer.Protocol.TF2E)
            {
                // TF2E data
                data.TF2EQueryInfo.buildName = "Test Build";
                data.TF2EQueryInfo.dataCenter = "Test Datacenter";
                data.TF2EQueryInfo.gameMode = "Test Game Mode";

                data.TF2EQueryInfo.basicInfo.port = GetPort();
                data.TF2EQueryInfo.basicInfo.platform = Application.platform.ToString();
                data.TF2EQueryInfo.basicInfo.playlistVersion = "N/A";
                data.TF2EQueryInfo.basicInfo.playlistNum = 0;

                data.TF2EQueryInfo.basicInfo.playlistName = "Team Deathmatch";
                data.TF2EQueryInfo.basicInfo.numClients = (byte)m_Players.Count;
                data.TF2EQueryInfo.basicInfo.maxClients = 16;
                data.TF2EQueryInfo.basicInfo.map = "Highrise";

                data.TF2EQueryInfo.performanceInfo.averageFrameTime = 1.0f;
                data.TF2EQueryInfo.performanceInfo.maxFrameTime = 2.0f;
                data.TF2EQueryInfo.performanceInfo.averageUserCommandTime = 3.0f;
                data.TF2EQueryInfo.performanceInfo.maxUserCommandTime = 4.0f;

                data.TF2EQueryInfo.matchState.phase = 0;
                data.TF2EQueryInfo.matchState.maxRounds = 3;
                data.TF2EQueryInfo.matchState.roundsWonIMC = 0;
                data.TF2EQueryInfo.matchState.roundsWonMilitia = 0;
                data.TF2EQueryInfo.matchState.timeLimitSeconds = 60;
                data.TF2EQueryInfo.matchState.timePassedSeconds = 5;
                data.TF2EQueryInfo.matchState.maxScore = 20;
                data.TF2EQueryInfo.matchState.teamsLeftWithPlayersNum = 2;

                TF2ETeam teamOne = new TF2ETeam();
                teamOne.id = 1;
                teamOne.score = 5;

                TF2ETeam teamTwo = new TF2ETeam();
                teamTwo.id = 2;
                teamTwo.score = 6;

                data.TF2EQueryInfo.teams.Add(teamOne);
                data.TF2EQueryInfo.teams.Add(teamTwo);

                foreach (Player player in m_Players)
                {
                    if (data.TF2EQueryInfo.basicInfo.platformPlayers.ContainsKey(player.platform))
                    {
                        // Increment the number of players on the platform if the platform exists
                        data.TF2EQueryInfo.basicInfo.platformPlayers[player.platform]++;
                    }
                    else
                    {
                        // Add the new platform with a player count of 1
                        data.TF2EQueryInfo.basicInfo.platformPlayers.Add(player.platform, 1);
                    }

                    TF2EClient client = new TF2EClient();
                    client.id = m_PlayerIndex;
                    client.name = player.username;
                    client.teamID = 1;
                    client.address = player.ip; // TODO: Test this with multiple clients on different networks. 
                    client.ping = 30;
                    client.packetsReceived = 0;
                    client.packetsDropped = 0;
                    client.score = 0;
                    client.kills = 0;
                    client.deaths = 0;

                    m_PlayerIndex++;

                    data.TF2EQueryInfo.clients.Add(client);
                }
            }

            m_PlayerIndex = 1;

            return data;
        }

        public override void OnStartServer()
        {
            Debug.Log("MyNetworkManager: Server Started!");

            m_SessionId = System.Guid.NewGuid().ToString();
            m_ServerQueryManager = GetComponent<ServerQueryManager>();

            m_ServerQueryManager.ServerStart(UpdateQueryData(), m_Protocol, m_QueryPort);
        }

        /// <summary>
        /// Deletes login information for user.
        /// </summary>
        internal void Logout()
        {
            isLoggedIn = false;
        }

        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            base.OnServerAddPlayer(conn);

            foreach (KeyValuePair<uint, NetworkIdentity> kvp in NetworkServer.spawned)
            {
                Player comp = kvp.Value.GetComponent<Player>();

                // Add to player list if new
                if (comp != null && !m_Players.Contains(comp))
                {
                    comp.sessionId = m_SessionId;
                    m_Players.Add(comp);
                }
            }
        }

        public override void OnStopServer()
        {
            Debug.Log("MyNetworkManager: Server Stopped!");
            m_ServerQueryManager.OnDestroy();
            m_SessionId = "";
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);

            Dictionary<uint, NetworkIdentity> spawnedPlayers = NetworkServer.spawned;
            
            // Update players list on client disconnect
            foreach (Player player in m_Players)
            {
                bool playerFound = false;

                foreach (KeyValuePair<uint, NetworkIdentity> kvp in spawnedPlayers)
                {
                    Player comp = kvp.Value.GetComponent<Player>();

                    // Verify the player is still in the match
                    if (comp != null && player == comp)
                    {
                        playerFound = true;
                        break;
                    }
                }

                if (!playerFound)
                {
                    m_Players.Remove(player);
                    break;
                }
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            Debug.Log("MyNetworkManager: Left the Server!");

            localPlayer = null;

            m_SessionId = "";
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            Debug.Log($"MyNetworkManager: {m_Username} Connected to Server!");
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            Debug.Log("MyNetworkManager: Disconnected from Server!");
        }

        /// <summary>
        /// Finds the local player if they are spawned in the scene.
        /// </summary>
        void FindLocalPlayer()
        {
            //Check to see if the player is loaded in yet
            if (NetworkClient.localPlayer == null)
                return;

            localPlayer = NetworkClient.localPlayer.GetComponent<Player>();
        }
    }
}

