﻿using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Vivox;
using Rpc;
using Unity.Helpers.ServerQuery.ServerQuery;
using Unity.Helpers.ServerQuery.Data;
using Unity.Helpers.ServerQuery;
using Unity.Helpers.ServerQuery.Protocols.SQP.Collections;
using Unity.Helpers.ServerQuery.Protocols.A2S.Collections;
using Unity.Helpers.ServerQuery.Protocols.TF2E.Collections;

namespace Network 
{
    public class MyNetworkManager : NetworkManager
    {
        public Player localPlayer;
        private VivoxManager m_VivoxManager;
        private ServerQueryManager m_ServerQueryManager;
        private UnityRpc m_UnityRpc;
        private string m_SessionId = "";
        private string m_Username;
        private string m_UserId;
        private bool m_IsDedicatedServer;
        private string m_Version = "001";
        public bool isLoggedIn = false;

        private List<Player> m_Players;

        public override void Awake()
        {
            base.Awake();
            m_Players = new List<Player>();
        }

        public override void Start()
        {
            base.Start();
            m_IsDedicatedServer = false;
            m_Username = SystemInfo.deviceName;
            m_UnityRpc = GetComponent<UnityRpc>();
            string[] args = System.Environment.GetCommandLineArgs();

            foreach(string arg in args)
            {
                if (arg == "-server")
                {
                    m_IsDedicatedServer = true;
                }
            }

            m_VivoxManager = GetComponent<VivoxManager>();
        }

        public void Login()
        {
            OnRequestCompleteDelegate<SignInResponse> loginDelegate = OnLoginComplete;
            m_UnityRpc.Login(m_Username, loginDelegate);
        }

        void OnLoginComplete(SignInResponse responseArgs, bool wasSuccessful)
        {
            if (wasSuccessful)
            {
                isLoggedIn = true;
                m_UserId = responseArgs.userid;

                m_UnityRpc.SetAuthToken(responseArgs.token);
                m_UnityRpc.SetPingSites(responseArgs.pingsites);

                m_UnityRpc.GetMultiplayEnvironment();
            }
        }

        public void RequestMatch()
        {
            OnPingSitesCompleteDelegate onPingCompleteDelegate = delegate ()
            {
                OnRequestCompleteDelegate<RequestMatchTicketResponse> RequestMatchDelegate = RequestMatchResponse;
                m_UnityRpc.GetRequestMatchTicket(1, RequestMatchDelegate);
            };
            m_UnityRpc.PingSites(onPingCompleteDelegate); // Ping sites needs delegate to know when all finished
        }


        public void CreateMatch()
        {
            m_UnityRpc.AllocateServer();
        }

        public void VivoxLogin()
        {
            m_VivoxManager.Login(m_UserId);
        }

        void RequestMatchResponse(RequestMatchTicketResponse responseArgs, bool wasSuccessful)
        {
            if (wasSuccessful)
            {
                OnRequestCompleteDelegate<MatchmakerPollingResponse> onMatchmakerPollingComplete = delegate (MatchmakerPollingResponse response, bool bWasSuccessful)
                {
                    if (bWasSuccessful)
                    {
                        Debug.Log($"successfully completed matchmaker polling, received connection: {response.assignment.connection}");
                    }
                };
                StartCoroutine(m_UnityRpc.PollMatch(responseArgs.id, responseArgs.token, onMatchmakerPollingComplete));
            }
        }

        private void Update()
        {
            if (NetworkManager.singleton.isNetworkActive)
            {
                if (localPlayer == null)
                {
                    FindLocalPlayer();
                    if(localPlayer != null) {
                        if (m_VivoxManager.isLoggedIn &&
                        localPlayer.sessionId != "")
                        {
                            OnJoinCompleteDelegate joinCompleteDelegate = delegate ()
                            {
                                m_VivoxManager.JoinChannel("TP_" + localPlayer.sessionId, VivoxUnity.ChannelType.Positional, true, false);
                            };
                            m_VivoxManager.JoinChannel("TN_" + localPlayer.sessionId, VivoxUnity.ChannelType.NonPositional, true, false, joinCompleteDelegate);
                        }
                        else
                        {
                            localPlayer = null;
                        }
                    }
                }
            }
            else
            {
                localPlayer = null;
                m_Players.Clear();
            }

        }

        internal void Logout()
        {
            m_UnityRpc.SetAuthToken("");
            isLoggedIn = false;
        }

        public override void OnStartServer()
        {
            Debug.Log("Server Started!");

            ServerQueryServer.Protocol protocol = ServerQueryServer.Protocol.SQP;
            ushort port = 0;

            string[] args = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-queryport")
                {
                    try
                    {
                        port = ushort.Parse(args[i+1]);
                        Debug.Log($"found port {port}");
                    }
                    catch 
                    {
                        Debug.Log($"unable to parse {args[i+1]} into ushort for port");
                    }
                }
                if (args[i] == "-queryprotocol")
                {
                    if(args[i+1] == "sqp")
                    {
                        protocol = ServerQueryServer.Protocol.SQP;
                    }
                    if (args[i + 1] == "a2s")
                    {
                        protocol = ServerQueryServer.Protocol.A2S;
                    }
                    if (args[i + 1] == "tf2e")
                    {
                        protocol = ServerQueryServer.Protocol.TF2E;
                    }
                    Debug.Log($"found query protocol: {args[i + 1]}");
                }
                if(args[i] == "-version")
                {
                    m_Version = args[i + 1];
                }
            }

            m_SessionId = System.Guid.NewGuid().ToString();
            m_ServerQueryManager = GetComponent<ServerQueryManager>();


            QueryData data = new QueryData();

            if (protocol == ServerQueryServer.Protocol.SQP)
            {
                // SQP Server Data
                data.SQPServerInfo.currentPlayers = 0;
                data.SQPServerInfo.maxPlayers = int.MaxValue;
                data.SQPServerInfo.serverName = "Default Server Name";
                data.SQPServerInfo.gameType = "Default Game Type";
                data.SQPServerInfo.buildID = "Default Build ID";
                data.SQPServerInfo.map = "Default Map Name";
                data.SQPServerInfo.gamePort = 0;

                // SQP Rule Data
                SQPServerRule rule = new SQPServerRule();
                rule.key = "TestRule";
                rule.type = (byte)SQPDynamicType.String;
                rule.valueString = "TestValue";

                data.SQPServerRules.rules.Add(rule);

                // SQP Player Data
                data.SQPPlayerInfo.playerCount = 2;
                data.SQPPlayerInfo.fieldCount = 1;

                SQPFieldKeyValue fieldPlayerOne = new SQPFieldKeyValue();
                fieldPlayerOne.key = "PlayerName";
                fieldPlayerOne.type = (byte)SQPDynamicType.String;
                fieldPlayerOne.valueString = "Jeff";

                SQPFieldKeyValue fieldPlayerTwo = new SQPFieldKeyValue();
                fieldPlayerTwo.key = "PlayerName";
                fieldPlayerTwo.type = (byte)SQPDynamicType.String;
                fieldPlayerTwo.valueString = "John";

                List<SQPFieldKeyValue> fieldsOne = new List<SQPFieldKeyValue>();
                fieldsOne.Add(fieldPlayerOne);
                List<SQPFieldKeyValue> fieldsTwo = new List<SQPFieldKeyValue>(); ;
                fieldsTwo.Add(fieldPlayerTwo);

                SQPFieldContainer playerOne = new SQPFieldContainer();
                playerOne.fields = fieldsOne;
                SQPFieldContainer playerTwo = new SQPFieldContainer();
                playerTwo.fields = fieldsTwo;

                data.SQPPlayerInfo.players.Add(playerOne);
                data.SQPPlayerInfo.players.Add(playerTwo);

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
            }

            if (protocol == ServerQueryServer.Protocol.A2S)
            {
                // A2S Server Data
                data.A2SServerInfo.protocol = 0;
                data.A2SServerInfo.serverName = "Default Server Name";
                data.A2SServerInfo.serverMap = "Default Scene";
                data.A2SServerInfo.folder = "C:/default";
                data.A2SServerInfo.gameName = "Unity Mirror Sample";
                data.A2SServerInfo.steamID = 12345;
                data.A2SServerInfo.playerCount = 2;
                data.A2SServerInfo.maxPlayers = byte.MaxValue;
                data.A2SServerInfo.botCount = 2;
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
                A2SPlayerResponsePacketPlayer player = new A2SPlayerResponsePacketPlayer();
                player.index = 0;
                player.playerName = "TestName";
                player.score = 23;
                player.duration = 23.5f;

                data.A2SPlayerInfo.numPlayers = 1;
                data.A2SPlayerInfo.players.Add(player);
            }

            if (protocol == ServerQueryServer.Protocol.TF2E)
            {
                data.TF2EQueryInfo.buildName = "Test Build";
                data.TF2EQueryInfo.dataCenter = "Test Datacenter";
                data.TF2EQueryInfo.gameMode = "Test Game Mode";

                data.TF2EQueryInfo.basicInfo.port = 7777;
                data.TF2EQueryInfo.basicInfo.platform = "PC";
                data.TF2EQueryInfo.basicInfo.playlistVersion = "N/A";
                data.TF2EQueryInfo.basicInfo.playlistNum = 0;

                data.TF2EQueryInfo.basicInfo.platformPlayers.Add("PC", 1);

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
                data.TF2EQueryInfo.matchState.roundsWonMilitia = 1;
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

                TF2EClient clientOne = new TF2EClient();
                clientOne.id = 1;
                clientOne.name = "Titanfall";
                clientOne.teamID = teamOne.id;
                clientOne.address = "127.0.0.1";
                clientOne.ping = 30;
                clientOne.packetsReceived = 100;
                clientOne.packetsDropped = 5;
                clientOne.score = 10;
                clientOne.kills = 11;
                clientOne.deaths = 1;

                TF2EClient clientTwo = new TF2EClient();
                clientTwo.id = 2;
                clientTwo.name = "Titangebackup";
                clientTwo.teamID = teamTwo.id;
                clientTwo.address = "127.0.0.1";
                clientTwo.ping = 40;
                clientTwo.packetsReceived = 105;
                clientTwo.packetsDropped = 10;
                clientTwo.score = 1;
                clientTwo.kills = 1;
                clientTwo.deaths = 10;

                data.TF2EQueryInfo.clients.Add(clientOne);
                data.TF2EQueryInfo.clients.Add(clientTwo);
            }


            m_ServerQueryManager.ServerStart(data, protocol, port);
        }

        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            base.OnServerAddPlayer(conn);
            // Update server query if any players are added to the server
            foreach (KeyValuePair<uint, NetworkIdentity> kvp in NetworkServer.spawned)
            {
                Player comp = kvp.Value.GetComponent<Player>();

                //Add if new
                if (comp != null && !m_Players.Contains(comp))
                {
                    comp.sessionId = m_SessionId;
                    m_Players.Add(comp);
                }
            }

            QueryData data = m_ServerQueryManager.GetQueryData();
            data.SQPServerInfo.currentPlayers = m_Players.Count;
            m_ServerQueryManager.UpdateQueryData(data);
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);
            Dictionary<uint, NetworkIdentity> spawnedPlayers = NetworkServer.spawned;

            // Update players list on client disconnect
            foreach(Player player in m_Players)
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

        public override void OnStopServer()
        {
            Debug.Log("Server Stopped!");
            m_ServerQueryManager.OnDestroy();
            m_SessionId = "";
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            Debug.Log("Left the Server!");
            m_VivoxManager.LeaveChannel();
            localPlayer = null;

            m_SessionId = "";
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            Debug.Log($"{m_VivoxManager.GetName()} connected to Server!");
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            Debug.Log("Disconnected from Server!");
        }

        void FindLocalPlayer()
        {
            //Check to see if the player is loaded in yet
            if (NetworkClient.localPlayer == null)
                return;

            localPlayer = NetworkClient.localPlayer.GetComponent<Player>();
        }
    }
}

