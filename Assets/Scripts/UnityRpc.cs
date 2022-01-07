using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;

namespace Rpc
{

    public delegate void OnPingSitesCompleteDelegate();

    public class UnityRpc : MonoBehaviour
    {
        /// <summary>
        /// Authorization token for the backend.
        /// </summary>
        private string m_AuthToken = "";

        //Replace UPID in the endpoint with the proper UPID once one is generated
        /// <summary>
        /// The matchmaker ticket endpoint.
        /// </summary>
        private string m_TicketEndpoint = "https://cloud.connected.unity3d.com/d98bfc46-6576-4059-b530-dc74eb4f1388/matchmaking/api/v1/tickets";

        /// <summary>
        /// The backend url.
        /// </summary>
        private string m_BackendUrl = "http://104.149.129.150:8080/rpc";
        private string m_LocalBackendUrl = "http://192.168.153.148:8080/rpc";

        /// <summary>
        /// Available ping sites.
        /// </summary>
        private PingInfo[] m_PingSites;

        /// <summary>
        /// Available multiplay profiles.
        /// </summary>
        private MultiplayProfile[] profiles;


        private bool m_MatchFound;
        private bool m_MatchCreated;
        private int m_TimePolling;
        private int m_PingsCompleted;
        private const int k_TimesToPing = 10;
        private float m_PingTimeout = 0.05f;

        /// <summary>
        /// Makes an allocation request to multiplay for a server
        /// </summary>
        public void AllocateServer()
        {
            MultiplayProfile profile = profiles[0];
            string[] profileInfo = profile.id.Split(char.Parse(":"));

            AllocateRequestParams allocateParams = new AllocateRequestParams();
            allocateParams.profile_id = profileInfo[0];
            allocateParams.fleet_id = profileInfo[1];
            allocateParams.region_id = profileInfo[2];

            OnRequestCompleteDelegate<AllocateResponse> onAllocateComplete = delegate (AllocateResponse response, bool wasSuccessful)
            {
                if (wasSuccessful && response.uuid != "")
                {
                    Debug.Log("AllocateServer: successfully allocated server");
                    StartCoroutine(PollMultiplay(response.uuid, allocateParams.fleet_id));
                }
                else
                {
                    Debug.Log("AllocateServer: failed allocating server");
                }
            };

            StartCoroutine(PostRequest<AllocateRequestParams, AllocateResponse>("MultiplayService.Allocate", allocateParams, onAllocateComplete));
        }

        /// <summary>
        /// Auth login for the backend.
        /// </summary>
        /// <param name="username">The username associated with the player.</param>
        /// <param name="onLoginCompleteDelegate">Callback delegate for when login is finished.</param>
        public void Login(string username, OnRequestCompleteDelegate<SignInResponse> onLoginCompleteDelegate)
        {
            SignInParams signInParams = new SignInParams();
            signInParams.username = username;
            StartCoroutine(PostRequest<SignInParams, SignInResponse>("AuthService.SignIn", signInParams, onLoginCompleteDelegate));
        }

        /// <summary>
        /// Retrieves a vivox login token from the backend.
        /// </summary>
        /// <param name="user">The username associated with the player.</param>
        /// <param name="onVivoxLoginTokenReceivedDelegate">Callback delegate for when vivox login token has been retrieved.</param>
        public void GetVivoxLoginToken(string user, OnRequestCompleteDelegate<VivoxTokenResponse> onVivoxLoginTokenReceivedDelegate)
        {
            VivoxTokenRequestParams vivoxRequestParams = new VivoxTokenRequestParams();
            vivoxRequestParams.user = user;
            StartCoroutine(PostRequest<VivoxTokenRequestParams, VivoxTokenResponse>("VivoxService.Login", vivoxRequestParams, onVivoxLoginTokenReceivedDelegate));
        }

        /// <summary>
        /// Retrieves a vivox join token from the backend.
        /// </summary>
        /// <param name="channelName">The name of the channel the player is trying to join.</param>
        /// <param name="channelType">The type of channel the player is trying to join.</param>
        /// <param name="onVivoxJoinTokenReceivedDelegate">Callback delegate for when vivox join token has been retrieved.</param>
        public void GetVivoxJoinToken(string channelName, string channelType, OnRequestCompleteDelegate<VivoxTokenResponse> onVivoxJoinTokenReceivedDelegate)
        {
            VivoxTokenRequestParams vivoxRequestParams = new VivoxTokenRequestParams();
            vivoxRequestParams.name = channelName;
            vivoxRequestParams.type = channelType;
            StartCoroutine(PostRequest<VivoxTokenRequestParams, VivoxTokenResponse>("VivoxService.Join", vivoxRequestParams, onVivoxJoinTokenReceivedDelegate));
        }

        /// <summary>
        /// Retrieves a match request ticket.
        /// </summary>
        /// <param name="mode">The game mode we are searching for.</param>
        /// <param name="onRequestMatchTicketReceivedDelegate">Callback delegate after match ticket has been retrieved.</param>
        public void GetRequestMatchTicket(int mode, OnRequestCompleteDelegate<RequestMatchTicketResponse> onRequestMatchTicketReceivedDelegate)
        {
            foreach (PingInfo site in m_PingSites)
            {
                site.packetloss = site.packetloss / k_TimesToPing;
            }

            RequestMatchTicketParams requestMatchTicketParams = new RequestMatchTicketParams();
            requestMatchTicketParams.mode = mode;
            requestMatchTicketParams.QosResults = m_PingSites;
            StartCoroutine(PostRequest<RequestMatchTicketParams, RequestMatchTicketResponse>("MatchMakerService.RequestMatch", requestMatchTicketParams, onRequestMatchTicketReceivedDelegate));
        }

        /// <summary>
        /// Polls multiplay for a server allocation.
        /// </summary>
        /// <param name="uuid">The uuid for the allocated server.</param>
        /// <param name="fleetId">The fleet id of the server.</param>
        public IEnumerator PollMultiplay(string uuid, string fleetId)
        {
            PollMultiplayParams pollMultiplayParams = new PollMultiplayParams();
            pollMultiplayParams.uuid = uuid;
            pollMultiplayParams.fleetid = fleetId;

            m_TimePolling = 0;
            m_MatchCreated = false;
            while (!m_MatchCreated && m_TimePolling < 300)
            {
                OnRequestCompleteDelegate<PollMultiplayResponse> onMultiplayPollComplete = delegate (PollMultiplayResponse response, bool wasSuccessful)
                {
                    if (!wasSuccessful || response.state == "Error" || response.status == "500")
                    {
                        // TODO: handle error here
                        Debug.Log($"PollMultiplay: failed retrieving connection from multiplay");
                        m_MatchCreated = true;
                    }
                    if (response.connection != "<nil>:0")
                    {
                        Debug.Log($"PollMultiplay: successfully retrieved connetion from multiplay: {response.connection}");
                        string test = response.connection;
                        m_MatchCreated = true;
                    }
                };
                StartCoroutine(PostRequest<PollMultiplayParams, PollMultiplayResponse>("MultiplayService.SingleAllocations", pollMultiplayParams, onMultiplayPollComplete));
                yield return new WaitForSeconds(5.0f);
                m_TimePolling += 5;
            }

            
        }

        /// <summary>
        /// Initiates polling of matchmaker.
        /// </summary>
        /// <param name="ticketId">Ticket Id of requested match.</param>
        /// <param name="delegateToken">Delegate token that was generated with match request.</param>
        /// <param name="onMatchmakerPollingCompleteDelegate">Callback delegate for when match has been found.</param>
        public IEnumerator PollMatch(string ticketId, string delegateToken, OnRequestCompleteDelegate<MatchmakerPollingResponse> onMatchmakerPollingCompleteDelegate)
        {
            m_TimePolling = 0;
            m_MatchFound = false;
            while (!m_MatchFound && m_TimePolling < 300)
            {
                StartCoroutine(PollMatchmaker(ticketId, delegateToken, onMatchmakerPollingCompleteDelegate));
                yield return new WaitForSeconds(5.0f);
                m_TimePolling += 5;
            }
        }

        /// <summary>
        /// Polls matchmaker for a match.
        /// </summary>
        /// <param name="ticketId">Ticket Id of requested match.</param>
        /// <param name="delegateToken">Delegate token that was generated with match request.</param>
        /// <param name="onMatchmakerPollingCompleteDelegate">Callback delegate for when match has been found.</param>
        public IEnumerator PollMatchmaker(string ticketId, string delegateToken, OnRequestCompleteDelegate<MatchmakerPollingResponse> onMatchmakerPollingCompleteDelegate)
        {
            string url = m_TicketEndpoint + "?id=" + ticketId;

            UnityWebRequest uwr = UnityWebRequest.Get(url);
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Accept", "application/json");
            uwr.SetRequestHeader("Authorization", "BEARER " + delegateToken);
            

            //Send the request then wait here until it returns
            yield return uwr.SendWebRequest();

            MatchmakerPollingResponse response = new MatchmakerPollingResponse();

            if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Error While Sending: " + uwr.error);
                m_MatchFound = true;
                onMatchmakerPollingCompleteDelegate(response, false);
            }
            else
            {
                Debug.Log("Received: " + uwr.downloadHandler.text);

                response = JsonUtility.FromJson<MatchmakerPollingResponse>(uwr.downloadHandler.text);
                if (response.assignment != null && response.assignment.connection != null)
                {
                    // call something to open the connection (will need delegate)
                    m_MatchFound = true;
                    onMatchmakerPollingCompleteDelegate(response, true);
                }
                if (response.assignment.error != null) 
                {
                    // propogate out error
                    m_MatchFound = true;
                    onMatchmakerPollingCompleteDelegate(response, true);
                }
            }
        }

        /// <summary>
        /// Retrieves multiplay environment from backend.
        /// </summary>
        public void GetMultiplayEnvironment()
        {
            EnvironmentRequestParams environmentRequestParams = new EnvironmentRequestParams();
            OnRequestCompleteDelegate<EnvironmentResponse> onEnvironmentReceived = delegate (EnvironmentResponse response, bool wasSuccessful)
            {
                if (wasSuccessful)
                {
                    Debug.Log("successfully retrieved multiplay Environment");
                    profiles = response.profiles;
                }
                else
                {
                    Debug.Log("failed to retrieve multiplay environment");
                }
            };
            StartCoroutine(PostRequest<EnvironmentRequestParams, EnvironmentResponse>("MultiplayService.Environment", environmentRequestParams, onEnvironmentReceived));
        }

        /// <summary>
        /// Generic method to send different types of post requests to the backend.
        /// </summary>
        /// <param name="method">Method to run on the backend.</param>
        /// <param name="requestParams">The request body params.</param>
        /// <param name="onRequestCompleteDelegate">Callback to use after a response has been recieved.</param>
        IEnumerator PostRequest<TRequest, TResponse>(string method, TRequest requestParams, OnRequestCompleteDelegate<TResponse> onRequestCompleteDelegate)
        {
            RequestArgs<TRequest> args = new RequestArgs<TRequest>();
            args.@params = requestParams;
            args.id = 1;
            args.jsonrpc = "2.0";
            args.method = method;

            string jsonData = JsonUtility.ToJson(args);
            Debug.Log($"JsonData: {jsonData}");

            UnityWebRequest uwr = UnityWebRequest.Post(m_BackendUrl, "POST");
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
            uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Accept", "application/json");
            if(m_AuthToken != "")
            {
                uwr.SetRequestHeader("Authorization", "BEARER " + m_AuthToken);
            }

            ResponseResult<TResponse> response = new ResponseResult<TResponse>();

            //Send the request then wait here until it returns
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Error While Sending: " + uwr.error);
                onRequestCompleteDelegate(response.result, false);
            }
            else
            {
                Debug.Log("Received: " + uwr.downloadHandler.text);

                response = JsonUtility.FromJson<ResponseResult<TResponse>>(uwr.downloadHandler.text);
                onRequestCompleteDelegate(response.result, true);
            }
        }

        /// <summary>
        /// Initiates Ping to all sites returned during auth login.
        /// </summary>
        /// <param name="onPingCompleteDelegate">Callback to use after all sites have been pinged.</param>
        public void PingSites(OnPingSitesCompleteDelegate onPingCompleteDelegate)
        {
            m_PingsCompleted = 0;
            foreach (PingInfo site in m_PingSites)
            {
                for(int i = 0; i < k_TimesToPing; i++)
                {
                    StartCoroutine(StartPing(site.ipv4, onPingCompleteDelegate));
                }
            }
        }

        /// <summary>
        /// Sends ping to the designated ip.
        /// </summary>
        /// <param name="ip">The ip address to ping.</param>
        /// <param name="onPingCompleteDelegate">Callback to use after all sites have been pinged.</param>
        IEnumerator StartPing(string ip, OnPingSitesCompleteDelegate onPingCompleteDelegate)
        {
            WaitForSeconds f = new WaitForSeconds(m_PingTimeout);
            Ping p = new Ping(ip);
            while (!p.isDone)
            {
                yield return f;
            }
            PingFinished(p, onPingCompleteDelegate);
        }

        /// <summary>
        /// Processes the completed ping.
        /// </summary>
        /// <param name="p">The finished ping.</param>
        /// <param name="onPingCompleteDelegate">Callback to use after all sites have been pinged.</param>
        void PingFinished(Ping p, OnPingSitesCompleteDelegate onPingCompleteDelegate)
        {
            if(p.time >= m_PingTimeout)
            {
                UpdatePingSitePacketLoss(p.ip);
            }
            else
            {
                UpdatePingSiteLatency(p.ip, p.time);
            }

            m_PingsCompleted++;

            if(m_PingsCompleted == m_PingSites.Length * k_TimesToPing)
            {
                Debug.Log("ping sites has completed");
                m_PingsCompleted = 0;
                onPingCompleteDelegate.Invoke();
            }
        }

        /// <summary>
        /// Updates a ping sites latency.
        /// </summary>
        /// <param name="ip">The ip address that was pinged.</param>
        /// <param name="time">The time(ms) the ping took.</param>
        void UpdatePingSiteLatency(string ip, int time)
        {
            PingInfo site = Array.Find<PingInfo>(m_PingSites, check => check.ipv4 == ip);
            site.latency = time;
        }

        /// <summary>
        /// Updates a ping sites packet loss. Only called when a ping fails.
        /// </summary>
        /// <param name="ip">The ip address that was pinged.</param>
        void UpdatePingSitePacketLoss(string ip)
        {
            PingInfo site = Array.Find<PingInfo>(m_PingSites, check => check.ipv4 == ip);
            site.packetloss++;
        }

        /// <summary>
        /// Sets auth token for the RPC.
        /// </summary>
        /// <param name="token">The auth token to set.</param>
        public void SetAuthToken(string token)
        {
            m_AuthToken = token;
        }

        /// <summary>
        /// Sets the RPCs ping sites.
        /// </summary>
        /// <param name="pingSites">An array of ping sites.</param>
        public void SetPingSites(PingInfo[] pingSites)
        {
            m_PingSites = pingSites;
        }
    }
}
