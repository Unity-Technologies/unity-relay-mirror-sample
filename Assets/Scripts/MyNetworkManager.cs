using System.Collections;
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
        public Player m_LocalPlayer;
        private VivoxManager m_VivoxManager;
        private ServerQueryManager sqpManager;
        private UnityRpc unityRpc;
        private string m_SessionId = "";
        private string m_Username;
        private string m_UserId;
        private bool isDedicatedServer;

        private List<Player> m_Players;

        public override void Awake()
        {
            base.Awake();
            m_Players = new List<Player>();
        }

        public override void Start()
        {
            base.Start();
            isDedicatedServer = false;
            m_Username = SystemInfo.deviceName;
            unityRpc = GetComponent<UnityRpc>();
            string[] args = System.Environment.GetCommandLineArgs();
            foreach(string arg in args)
            {
                if (arg == "-server")
                {
                    isDedicatedServer = true;
                }
            }
        }

        public void Login()
        {
            OnRequestCompleteDelegate<SignInResponse> loginDelegate = OnLoginComplete;
            unityRpc.Login(m_Username, loginDelegate);
        }

        void OnLoginComplete(SignInResponse responseArgs, bool wasSuccessful)
        {
            if (wasSuccessful)
            {
                m_UserId = responseArgs.userid;

                unityRpc.SetAuthToken(responseArgs.token);
                unityRpc.SetPingSites(responseArgs.pingsites);

                unityRpc.GetEnvironment();
            }
        }

        public void RequestMatch()
        {
            OnPingSitesCompleteDelegate onPingCompleteDelegate = delegate ()
            {
                OnRequestCompleteDelegate<RequestMatchTicketResponse> RequestMatchDelegate = RequestMatchResponse;
                unityRpc.GetRequestMatchTicket(1, RequestMatchDelegate);
            };
            unityRpc.PingSites(onPingCompleteDelegate); // Ping sites needs delegate to know when all finished
        }


        public void CreateMatch()
        {
            unityRpc.AllocateServer();
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
                StartCoroutine(unityRpc.PollMatch(responseArgs.id, responseArgs.token, onMatchmakerPollingComplete));
            }
        }

        private void Update()
        {
            if (NetworkManager.singleton.isNetworkActive)
            {

                if (m_LocalPlayer == null)
                {
                    FindLocalPlayer();
                    //player was found, therefore we have spawned into the game
                    if(m_LocalPlayer != null)
                    {
                        if (m_VivoxManager.IsLoggedIn)
                        {
                            OnJoinCompleteDelegate joinCompleteDelegate = delegate ()
                            {
                                m_VivoxManager.JoinChannel("TP_" + m_LocalPlayer.m_SessionId, VivoxUnity.ChannelType.Positional, true, false);
                            };
                            m_VivoxManager.JoinChannel("TN_" + m_LocalPlayer.m_SessionId, VivoxUnity.ChannelType.NonPositional, true, false, joinCompleteDelegate);
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
                m_LocalPlayer = null;
                m_Players.Clear();
            }
        }
        public override void OnStartServer()
        {
            Debug.Log("Server Started!");

            SQPServer.Protocol protocol = SQPServer.Protocol.TF2E;
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
                        protocol = SQPServer.Protocol.SQP;
                    }
                    if (args[i + 1] == "a2s")
                    {
                        protocol = SQPServer.Protocol.A2S;
                    }
                    Debug.Log($"found query protocol: {args[i + 1]}");
                }
                if (args[i] == "-version")
                {
                    version = args[i + 1];
                }
            }

            m_SessionId = System.Guid.NewGuid().ToString();
            sqpManager = GetComponent<ServerQueryManager>();


            QueryData data = new QueryData();
            data.ServerInfo.CurrentPlayers = m_Players.Count;
            data.ServerInfo.GameType = "slayer";
            data.ServerInfo.ServerName = "testing sqp";
            data.ServerInfo.MaxPlayers = 20;
            sqpManager.ServerStart(data, SQPServer.Protocol.TF2E, 9000);

            // Checking for TF2E as it is not implemented yet -- TODO:change this later
            if (port != 0 && protocol != SQPServer.Protocol.TF2E)
            {
                sqpManager.ServerStart(data, protocol, port);
            }
        }

        public override void OnStopServer()
        {
            Debug.Log("Server Stopped!");
            sqpManager.OnDestroy();
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

        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            base.OnServerAddPlayer(conn);
            foreach (KeyValuePair<uint, NetworkIdentity> kvp in NetworkIdentity.spawned)
            {
                Player comp = kvp.Value.GetComponent<Player>();

                //Add if new
                if (comp != null && !m_Players.Contains(comp))
                {
                    comp.m_SessionId = m_SessionId;
                    m_Players.Add(comp);
                }
            }

            QueryData data = sqpManager.GetQueryData();
            data.ServerInfo.CurrentPlayers = m_Players.Count;
            sqpManager.UpdateQueryData(data);
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            Debug.Log("Disconnected from Server!");
            m_VivoxManager.LeaveChannel(null);
        }

        void FindLocalPlayer()
        {
            //Check to see if the player is loaded in yet
            if (ClientScene.localPlayer == null)
                return;

            m_LocalPlayer = ClientScene.localPlayer.GetComponent<Player>();
        }
    }
}

