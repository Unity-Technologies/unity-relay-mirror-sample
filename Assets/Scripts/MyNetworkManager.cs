using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Vivox;
using Rpc;

namespace Network 
{
    public class MyNetworkManager : NetworkManager
    {
        public Player m_LocalPlayer;
        private VivoxManager m_VivoxManager;
        private UnityRpc unityRpc;
        private string m_SessionId = "";
        private string m_Username;
        private string m_AuthToken;
        private string m_UserId;
        private bool isDedicatedServer;

        private List<Player> m_Players;

        public override void Start()
        {
            base.Start();
            isDedicatedServer = false;
            m_Username = SystemInfo.deviceName;
            unityRpc = GetComponent<UnityRpc>();
            m_Players = new List<Player>();
            string[] args = System.Environment.GetCommandLineArgs();
            foreach(string arg in args)
            {
                if (arg == "-server")
                {
                    isDedicatedServer = true;
                }
            }
            if (!isDedicatedServer)
            {
                Login();
            }
        }

        void Login()
        {
            //m_VivoxManager = GetComponent<VivoxManager>();
            OnRequestCompleteDelegate<SignInResponse> loginDelegate = OnLoginComplete;
            unityRpc.Login(m_Username, loginDelegate);
        }

        void OnLoginComplete(SignInResponse responseArgs, bool wasSuccessful)
        {
            if (wasSuccessful)
            {
                m_AuthToken = responseArgs.token;
                m_UserId = responseArgs.userid;

                unityRpc.SetAuthToken(m_AuthToken);
                unityRpc.SetPingSites(responseArgs.pingsites);

                OnPingSitesCompleteDelegate onPingCompleteDelegate = delegate ()
                {
                    OnRequestCompleteDelegate<RequestMatchTicketResponse> RequestMatchDelegate = RequestMatchResponse;
                    // TODO: fix multiplay polling/ request match. Getting validation error with polling
                    unityRpc.GetRequestMatchTicket(1, m_AuthToken, RequestMatchDelegate);
                };
                unityRpc.PingSites(onPingCompleteDelegate); // Ping sites needs delegate to know when all finished

                unityRpc.GetEnvironment(m_AuthToken);
                //m_VivoxManager.Login(m_UserId);
            }
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
                    unityRpc.AllocateServer();
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
                                //m_VivoxManager.JoinChannel("TP_" + m_LocalPlayer.m_SessionId, VivoxUnity.ChannelType.Positional, true, false);
                            };
                            //m_VivoxManager.JoinChannel("TN_" + m_LocalPlayer.m_SessionId, VivoxUnity.ChannelType.NonPositional, true, false, joinCompleteDelegate);
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
            m_SessionId = System.Guid.NewGuid().ToString();
        }

        public override void OnStopServer()
        {
            Debug.Log("Server Stopped!");
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

