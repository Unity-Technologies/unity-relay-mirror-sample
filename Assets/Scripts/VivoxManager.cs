using UnityEngine;

using System.ComponentModel;
using System;

using VivoxUnity;
using Rpc;


namespace Vivox
{
    public delegate void OnJoinCompleteDelegate();

    public class VivoxManager : MonoBehaviour
    {

        private UnityRpc unityRpc;

        private Uri endpoint;

        private string m_UserId;

        /// <summary>
        /// Flag to identify if the user is logged into vivox.
        /// </summary>
        public bool isLoggedIn;

#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
        private VivoxUnity.Client client;

        private ILoginSession m_LoginSession;
        private IChannelSession m_ChannelSession;
        private ChannelId m_CurrentChannelId;

        private ChannelId m_PositionalChannelId;
        private ChannelId m_NonPositionalChannelId;
#endif

        /// <summary>
        /// Initializing function for vivox manager.
        /// </summary>
        /// <param name="logLevel">Logging level to initialize vivox with.</param>
        internal void Init(int logLevel)
        {
#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
            isLoggedIn = false;
            unityRpc = GetComponent<UnityRpc>();
            client = new VivoxUnity.Client();

            // Uninitialize to clean up any old instances
            client.Uninitialize();

            VivoxConfig config = new VivoxConfig();
            config.InitialLogLevel = (vx_log_level)logLevel;
            client.Initialize(config);
            DontDestroyOnLoad(this);
#endif
        }

        private void OnApplicationQuit()
        {
#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
            client.Uninitialize();
#endif
        }

        /// <summary>
        /// Binds the callbacks for Vivox Login status.
        /// </summary>
        /// <param name="bind">Bool whether to bind or unbind the callback.</param>
        /// <param name="loginSession">The login session to bind/unbind with.</param>
        private void BindLoginCallbackListeners(bool bind, ILoginSession loginSession)
        {
            if (bind)
            {
                loginSession.PropertyChanged += OnLoginStatusChanged;
            }
            else
            {
                loginSession.PropertyChanged -= OnLoginStatusChanged;
            }
        }

        /// <summary>
        /// Binds the callbacks for Vivox channel status.
        /// </summary>
        /// <param name="bind">Bool whether to bind or unbind the callback.</param>
        /// <param name="channelSession">The channel session to bind/unbind with.</param>
        private void BindChannelCallbackListeners(bool bind, IChannelSession channelSession)
        {
            if (bind)
            {
                channelSession.PropertyChanged += OnChannelStatusChanged;
            }
            else
            {
                channelSession.PropertyChanged -= OnChannelStatusChanged;
            }
        }

        /// <summary>
        /// Logs into the Vivox SDK.
        /// </summary>
        /// <param name="username">The username to login with.</param>
        public void Login(string username)
        {
#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
            OnRequestCompleteDelegate<VivoxTokenResponse> vivoxLoginTokenReceived = delegate (VivoxTokenResponse responseArgs, bool wasSuccessful)
            {
                m_UserId = username;
                endpoint = new Uri(responseArgs.endpoint);
                AccountId accountId = new AccountId(responseArgs.uri);
                m_LoginSession = client.GetLoginSession(accountId);
                BindLoginCallbackListeners(true, m_LoginSession);
                m_LoginSession.BeginLogin(endpoint, responseArgs.token, ar =>
                {
                    try
                    {
                        m_LoginSession.EndLogin(ar);
                    }
                    catch (Exception e)
                    {
                        BindLoginCallbackListeners(false, m_LoginSession);
                        Debug.Log(e.Message);
                        isLoggedIn = false;
                    }
                });
            };
            unityRpc.GetVivoxLoginToken(username, vivoxLoginTokenReceived);
#endif
        }

        /// <summary>
        /// Logs out of Vivox SDK.
        /// </summary>
        public void Logout()
        {
#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
            m_LoginSession.Logout();
            BindLoginCallbackListeners(false, m_LoginSession);
            isLoggedIn = false;
#endif
        }

        /// <summary>
        /// Callback function for logging changes to Vivox Login status.
        /// </summary>
        /// <param name="sender">the sender of the callback.</param>
        /// <param name="loginArgs">The login arguments.</param>
        private void OnLoginStatusChanged(object sender, PropertyChangedEventArgs loginArgs)
        {
#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
            var source = (ILoginSession)sender;

            switch (source.State)
            {
                case LoginState.LoggingIn:
                    Debug.Log("Logging In");
                    break;

                case LoginState.LoggedIn:
                    Debug.Log($"Logged In {m_LoginSession.LoginSessionId.Name}");
                    isLoggedIn = true;
                    break;

                    // no case for logged out. Vivox Logout function changes the state for logout but doesn't send events for it
            }
#endif
        }

        /// <summary>
        /// Uses the Vivox SDK to Join voice and text channels.
        /// </summary>
        /// <param name="channelName">The name of the channel to join.</param>
        /// <param name="channelType">The channel type.</param>
        /// <param name="connectAudio">Flag to connect to audio.</param>
        /// <param name="connectText">Flag to connect to text.</param>
        /// <param name="joinCompleteDelegate">Callback used for when Join is complete.</param>
        /// <param name="transmissionSwitch">Flag to switch channels automatically when joining another channel.</param>
        /// <param name="properties">3D channel properties.</param>
        public void JoinChannel(string channelName, ChannelType channelType, bool connectAudio, bool connectText, OnJoinCompleteDelegate joinCompleteDelegate = null, bool transmissionSwitch = true, Channel3DProperties properties = null)
        {
#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
            OnRequestCompleteDelegate<VivoxTokenResponse> vivoxJoinTokenReceived = delegate (VivoxTokenResponse responseArgs, bool wasSuccessful)
            {
                m_CurrentChannelId = new ChannelId(responseArgs.uri);
                m_ChannelSession = m_LoginSession.GetChannelSession(m_CurrentChannelId);
                BindChannelCallbackListeners(true, m_ChannelSession);

                if (connectAudio)
                {
                    m_ChannelSession.PropertyChanged += OnAudioStateChanged;
                }
                if (connectText)
                {
                    m_ChannelSession.PropertyChanged += OnTextStateChanged;
                }

                m_ChannelSession.BeginConnect(connectAudio, connectText, transmissionSwitch, responseArgs.token, ar =>
                {
                    try
                    {
                        m_ChannelSession.EndConnect(ar);
                        if (channelType == ChannelType.NonPositional)
                        {
                            m_NonPositionalChannelId = m_CurrentChannelId;
                        }
                        else // Positional
                        {
                            m_PositionalChannelId = m_CurrentChannelId;
                        }
                    }
                    catch (Exception e)
                    {
                        BindChannelCallbackListeners(false, m_ChannelSession);
                        if (connectAudio)
                        {
                            m_ChannelSession.PropertyChanged -= OnAudioStateChanged;
                        }
                        if (connectText)
                        {
                            m_ChannelSession.PropertyChanged -= OnTextStateChanged;
                        }
                        Debug.Log("join failed: " + e.Message);
                    }
                    finally
                    {
                        if (joinCompleteDelegate != null)
                        {
                            joinCompleteDelegate.Invoke();
                        }
                    }
                });
            };
            unityRpc.GetVivoxJoinToken(channelName, channelType.ToString(), vivoxJoinTokenReceived);
#endif
        }

        /// <summary>
        /// Callback function for logging changes to Vivox channel status.
        /// </summary>
        /// <param name="sender">the sender of the callback.</param>
        /// <param name="channelArgs">The channel arguments.</param>
        private void OnChannelStatusChanged(object sender, PropertyChangedEventArgs channelArgs)
        {
#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
            IChannelSession source = (IChannelSession)sender;

            switch (source.ChannelState)
            {
                case ConnectionState.Connecting:
                    Debug.Log("Channel Connecting");
                    break;
                case ConnectionState.Connected:
                    Debug.Log($"Connected to channel {source.Channel.Name}");
                    break;
                case ConnectionState.Disconnecting:
                    Debug.Log($"Disconnecting from channel {source.Channel.Name}");
                    break;
                case ConnectionState.Disconnected:
                    Debug.Log($"Disconnected from channel {source.Channel.Name}");
                    break;
            }
#endif
        }

        /// <summary>
        /// Uses Vivox SDK to leave channels.
        /// </summary>
        public void LeaveChannel()
        {
#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
            m_ChannelSession.Disconnect();
            if (m_NonPositionalChannelId != null)
            {
                m_LoginSession.DeleteChannelSession(m_NonPositionalChannelId);
            }
            if (m_PositionalChannelId != null)
            {
                m_LoginSession.DeleteChannelSession(m_PositionalChannelId);
            }

            m_NonPositionalChannelId = new ChannelId("");
            m_PositionalChannelId = new ChannelId("");
            m_CurrentChannelId = new ChannelId("");
#endif
        }

        /// <summary>
        /// Callback function for logging changes to audio state.
        /// </summary>
        /// <param name="sender">the sender of the callback.</param>
        /// <param name="audioArgs">The audio arguments.</param>
        private void OnAudioStateChanged(object sender, PropertyChangedEventArgs audioArgs)
        {
#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
            IChannelSession source = (IChannelSession)sender;

            switch(source.AudioState)
            {
                case ConnectionState.Connecting:
                    Debug.Log("Audio Channel Connecting");
                    break;
                case ConnectionState.Connected:
                    Debug.Log("Audio Channel Connected");
                    break;
                case ConnectionState.Disconnecting:
                    Debug.Log("Audio Channel Disconnecting");
                    break;
                case ConnectionState.Disconnected:
                    Debug.Log("Audio Channel Disconnected");
                    break;
            }
#endif
        }

        /// <summary>
        /// Callback function for logging changes to text state.
        /// </summary>
        /// <param name="sender">the sender of the callback.</param>
        /// <param name="textArgs">The text arguments.</param>
        private void OnTextStateChanged(object sender, PropertyChangedEventArgs textArgs)
        {
#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
            IChannelSession source = (IChannelSession)sender;

            switch(source.TextState)
            {
                case ConnectionState.Connecting:
                    Debug.Log("Text Channel Connecting");
                    break;
                case ConnectionState.Connected:
                    Debug.Log("Text Channel Connected");
                    break;
                case ConnectionState.Disconnecting:
                    Debug.Log("Text Channel Disconnecting");
                    break;
                case ConnectionState.Disconnected:
                    Debug.Log("Text Channel Disconnected");
                    break;
            }
#endif
        }

        /// <summary>
        /// Retrieves user ID used to log into Vivox.
        /// </summary>
        public string GetName()
        {
            return m_UserId;
        }

        /// <summary>
        /// Sends updates to the Vivox SDK of the users 3D positon for 3D channels.
        /// </summary>
        /// <param name="curPosition">Current position in 3D space.</param>
        public void Update3DPosition(Transform curPosition)
        {
#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
            if (m_CurrentChannelId == m_PositionalChannelId)
            {
                m_ChannelSession.Set3DPosition(curPosition.position, curPosition.position, curPosition.forward, curPosition.up);
            }
#endif
        }

        /// <summary>
        /// Returns the current audio state.
        /// </summary>
        public ConnectionState GetAudioState()
        {
#if PLATFORM_STANDALONE_LINUX || UNITY_STANDALONE_LINUX
            return ConnectionState.Disconnected;
#else
            if(m_ChannelSession != null)
            {
                return m_ChannelSession.AudioState;
            }
            else
            {
                return ConnectionState.Disconnected;
            }
#endif
        }

        /// <summary>
        /// Changes transmission mode when in mutliple channels.
        /// </summary>
        /// <param name="keyCode">The key code pressed.</param>
        public void ChangeChannel(KeyCode keyCode)
        {
#if !PLATFORM_STANDALONE_LINUX && !UNITY_STANDALONE_LINUX
            if(keyCode == KeyCode.N && m_CurrentChannelId != m_PositionalChannelId)
            {
                Debug.Log($"Switching from non positional channel {m_CurrentChannelId} to positional channel {m_PositionalChannelId}");
                m_LoginSession.SetTransmissionMode(TransmissionMode.Single, m_PositionalChannelId);
                m_CurrentChannelId = m_PositionalChannelId;
            }
            if(keyCode == KeyCode.M && m_CurrentChannelId != m_NonPositionalChannelId)
            {
                Debug.Log($"Switching from positional channel {m_CurrentChannelId} to non positional channel {m_NonPositionalChannelId}");
                m_LoginSession.SetTransmissionMode(TransmissionMode.Single, m_NonPositionalChannelId);
                m_CurrentChannelId = m_NonPositionalChannelId;
            }
#endif
        }
    }

}
