// vis2k: GUILayout instead of spacey += ...; removed Update hotkeys to avoid
// confusion if someone accidentally presses one.
using System.ComponentModel;
using UnityEngine;
using Network;
using Mirror;
using Rpc;

namespace UI
{
    /// <summary>
    /// An extension for the NetworkManager that displays a default HUD for controlling the network state of the game.
    /// <para>This component also shows useful internal state for the networking system in the inspector window of the editor. It allows users to view connections, networked objects, message handlers, and packet statistics. This information can be helpful when debugging networked games.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/MenuUI")]
    [RequireComponent(typeof(MyNetworkManager))]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [HelpURL("https://mirror-networking.com/docs/Components/NetworkManagerHUD.html")]
    public class MenuUI : MonoBehaviour
    {
        private MyNetworkManager m_Manager;

        /// <summary>
        /// Whether to show the default control HUD at runtime.
        /// </summary>
        public bool showGUI = true;

        /// <summary>
        /// The horizontal offset in pixels to draw the HUD runtime GUI at.
        /// </summary>
        public int offsetX;

        /// <summary>
        /// The vertical offset in pixels to draw the HUD runtime GUI at.
        /// </summary>
        public int offsetY;

        void Awake()
        {
            m_Manager = GetComponent<MyNetworkManager>();
        }

        void OnGUI()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg == "-server")
                {
                    m_Manager.StartServer();
                    showGUI = false;
                }
            }

            if (!showGUI)
                return;

            GUILayout.BeginArea(new Rect(10 + offsetX, 40 + offsetY, 215, 9999));
            if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
            }

            // client ready
            if (NetworkClient.isConnected && !NetworkClient.ready)
            {
                NetworkClient.Ready();

                if (NetworkClient.localPlayer == null)
                {
                    NetworkClient.AddPlayer();
                }

            }

            StopButtons();

            GUILayout.EndArea();
        }

        void StartButtons()
        {
            if (!NetworkClient.active)
            {
                // Server + Client
                if (Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    if (GUILayout.Button("Host (Server + Client)"))
                    {
                        m_Manager.StartHost();
                    }
                }

                // Client + IP
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Client"))
                {
                    m_Manager.StartClient();
                }
                m_Manager.networkAddress = GUILayout.TextField(m_Manager.networkAddress);
                GUILayout.EndHorizontal();

                // Server Only
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    // cant be a server in webgl build
                    GUILayout.Box("(  WebGL cannot be server  )");
                }
                else
                {
                    if (GUILayout.Button("Server Only")) m_Manager.StartServer();
                }

                if (m_Manager.isLoggedIn)
                {
                    if (GUILayout.Button("Auth Logout"))
                    {
                        m_Manager.Logout();
                    }
                    if (GUILayout.Button("Vivox Login"))
                    {
                        m_Manager.VivoxLogin();
                    }
                    if (GUILayout.Button("Start Matchmaking"))
                    {
                        m_Manager.RequestMatch();
                    }
                    if (GUILayout.Button("Request Multiplay Server"))
                    {
                        m_Manager.CreateMatch();
                    }
                }
                else
                {
                    if (GUILayout.Button("Auth Login"))
                    {
                        m_Manager.Login();
                    }
                }
            }
            else
            {
                // Connecting
                GUILayout.Label("Connecting to " + m_Manager.networkAddress + "..");
                if (GUILayout.Button("Cancel Connection Attempt"))
                {
                    m_Manager.StopClient();
                }
            }
        }

        void StatusLabels()
        {
            // server / client status message
            if (NetworkServer.active)
            {
                GUILayout.Label("Server: active. Transport: " + Transport.activeTransport);
            }
            if (NetworkClient.isConnected)
            {
                GUILayout.Label("Client: address=" + m_Manager.networkAddress);
            }
        }

        void StopButtons()
        {
            // stop host if host mode
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                if (GUILayout.Button("Stop Host"))
                {
                    m_Manager.StopHost();
                }
            }
            // stop client if client-only
            else if (NetworkClient.isConnected)
            {
                if (GUILayout.Button("Stop Client"))
                {
                    m_Manager.StopClient();
                }
            }
            // stop server if server-only
            else if (NetworkServer.active)
            {
                if (GUILayout.Button("Stop Server"))
                {
                    m_Manager.StopServer();
                }
            }
        }
    }
}
