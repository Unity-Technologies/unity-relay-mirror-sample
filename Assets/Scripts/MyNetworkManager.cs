using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Vivox;
using Rpc;
using Unity.Helpers.ServerQuery.ServerQuery;
using Unity.Helpers.ServerQuery.Data;
using Unity.Helpers.ServerQuery;

namespace Network 
{
    public class MyNetworkManager : NetworkManager
    {
        public Player localPlayer;
        private VivoxManager m_VivoxManager;
        private ServerQueryManager m_SQPManager;
        private UnityRpc m_UnityRpc;
        private string m_SessionId = "";
        private string m_Username;
        private string m_UserId;
        private bool m_IsDedicatedServer;
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
            m_VivoxManager = GetComponent<VivoxManager>();
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
                    //player was found, therefore we have spawned into the game
                    if(localPlayer != null)
                    {
                        if (m_VivoxManager.IsLoggedIn)
                        {
                            OnJoinCompleteDelegate joinCompleteDelegate = delegate ()
                            {
                                m_VivoxManager.JoinChannel("TP_" + localPlayer.sessionId, VivoxUnity.ChannelType.Positional, true, false);
                            };
                            m_VivoxManager.JoinChannel("TN_" + localPlayer.sessionId, VivoxUnity.ChannelType.NonPositional, true, false, joinCompleteDelegate);
                        }
                        else
                        {
                            Debug.LogWarning("Can't join Vivox channels are we are not logged in");
                        }
                    }
                }
            }
            else
            {
                localPlayer = null;
                m_Players.Clear();
            }

            // Update server query if any players are added to the server
            if (m_IsDedicatedServer)
            {
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

                QueryData data = m_SQPManager.GetQueryData();
                data.ServerInfo.CurrentPlayers = m_Players.Count;
                m_SQPManager.UpdateQueryData(data);
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
            string version = "";

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
                if (args[i] == "-version")
                {
                    version = args[i + 1];
                }
            }

            m_SessionId = System.Guid.NewGuid().ToString();
            m_SQPManager = GetComponent<ServerQueryManager>();


            QueryData data = new QueryData();
            data.ServerInfo.CurrentPlayers = 2;
            data.ServerInfo.GameType = "N/A";
            data.ServerInfo.ServerName = "Unity Dedicated Server";
            data.ServerInfo.Map = "Tutorial Map";
            data.ServerInfo.BuildID = "001";
            data.ServerInfo.GamePort = 1234;
            data.ServerInfo.MaxPlayers = 16;
            m_SQPManager.ServerStart(data, protocol, port);
        }

        public override void OnStopServer()
        {
            Debug.Log("Server Stopped!");
            m_SQPManager.OnDestroy();
            m_SessionId = "";
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            Debug.Log("Left the Server!");
            m_VivoxManager.LeaveChannel(null);
            m_SessionId = "";
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            Debug.Log($"{m_VivoxManager.GetName()} connected to Server!");
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            Debug.Log("Disconnected from Server!");
            m_VivoxManager.LeaveChannel(null);
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

