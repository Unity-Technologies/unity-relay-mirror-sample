using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VivoxUnity;
using System.ComponentModel;
using System;
using Rpc;

namespace Vivox
{
    public delegate void OnJoinCompleteDelegate();

    public class VivoxManager : MonoBehaviour
    {
        VivoxUnity.Client client;
        UnityRpc unityRpc;

        private Uri endpoint;

        private string m_UserId;

        private ILoginSession m_LoginSession;
        private IChannelSession m_ChannelSession;
        ChannelId m_CurrentChannelId;
        public bool isLoggedIn;

        private ChannelId m_PositionalChannelId;
        private ChannelId m_NonPositionalChannelId;

        private void Awake()
        {
            isLoggedIn = false;
            unityRpc = GetComponent<UnityRpc>();
            client = new VivoxUnity.Client();

            // Uninitialize to clean up any old instances
            client.Uninitialize();

            //TODO: change to make log level dynamic
            VivoxConfig config = new VivoxConfig();
            config.InitialLogLevel = (vx_log_level)2;
            client.Initialize(config);
            DontDestroyOnLoad(this);
        }

        private void OnApplicationQuit()
        {
            client.Uninitialize();
        }

        public void BindLoginCallbackListeners(bool bind, ILoginSession LoginSession)
        {
            if (bind)
            {
                LoginSession.PropertyChanged += OnLoginStatusChanged;
            }
            else
            {
                LoginSession.PropertyChanged -= OnLoginStatusChanged;
            }
        }

        public void BindChannelCallbackListeners(bool bind, IChannelSession channelSession)
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

        public void Login(string username)
        {
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
        }

        public void Logout()
        {
            m_LoginSession.Logout();
            BindLoginCallbackListeners(false, m_LoginSession);
            isLoggedIn = false;
        }

        public void OnLoginStatusChanged(object sender, PropertyChangedEventArgs loginArgs)
        {
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
        }

        public void JoinChannel(string channelName, ChannelType channelType, bool connectAudio, bool connectText, OnJoinCompleteDelegate joinCompleteDelegate = null, bool transmissionSwitch = true, Channel3DProperties properties = null)
        {
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
        }

        public void OnChannelStatusChanged(object sender, PropertyChangedEventArgs channelArgs)
        {
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
        }

        public void LeaveChannel()
        {
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

        }

        public void OnAudioStateChanged(object sender, PropertyChangedEventArgs audioArgs)
        {
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
        }

        public void OnTextStateChanged(object sender, PropertyChangedEventArgs textArgs)
        {
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
        }

        public string GetName()
        {
            return m_UserId;
        }

        public void Update3DPosition(Transform curPosition)
        {
            if (m_CurrentChannelId == m_PositionalChannelId)
            {
                m_ChannelSession.Set3DPosition(curPosition.position, curPosition.position, curPosition.forward, curPosition.up);
            }
        }

        public ConnectionState GetAudioState()
        {
            if(m_ChannelSession != null)
            {
                return m_ChannelSession.AudioState;
            }
            else
            {
                return ConnectionState.Disconnected;
            }
        }

        public void ChangeChannel(KeyCode keyCode)
        {
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
        }
    }

}
