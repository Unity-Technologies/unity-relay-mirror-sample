using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;

namespace Rpc
{

    public delegate void OnPingSitesCompleteDelegate();

    public class UnityRpc : MonoBehaviour
    {
        private string m_AuthToken = "";

        //Replace UPID in the endpoint with the proper UPID once one is generated
        private string m_TicketEndpoint = "https://cloud.connected.unity3d.com/d98bfc46-6576-4059-b530-dc74eb4f1388/matchmaking/api/v1/tickets";

        private string m_BackendUrl = "http://104.149.129.150:8080/rpc";
        private string m_LocalBackendUrl = "http://172.23.218.171:8080/rpc";

        private PingInfo[] m_PingSites;
        private MultiplayProfile[] profiles;

        private bool m_MatchFound;
        private bool m_MatchCreated;
        private int m_TimePolling;
        private int m_PingsCompleted;
        private float m_PingTimeout = 0.05f;

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

        public void Login(string username, OnRequestCompleteDelegate<SignInResponse> onLoginCompleteDelegate)
        {
            SignInParams signInParams = new SignInParams();
            signInParams.username = username;
            StartCoroutine(PostRequest<SignInParams, SignInResponse>("AuthService.SignIn", signInParams, onLoginCompleteDelegate));
        }

        public void GetVivoxLoginToken(string user, OnRequestCompleteDelegate<VivoxTokenResponse> onVivoxLoginTokenReceived)
        {
            VivoxTokenRequestParams vivoxRequestParams = new VivoxTokenRequestParams();
            vivoxRequestParams.user = user;
            StartCoroutine(PostRequest<VivoxTokenRequestParams, VivoxTokenResponse>("VivoxService.Login", vivoxRequestParams, onVivoxLoginTokenReceived));
        }

        public void GetVivoxJoinToken(string channelName, string channelType, OnRequestCompleteDelegate<VivoxTokenResponse> onVivoxLoginTokenReceived)
        {
            VivoxTokenRequestParams vivoxRequestParams = new VivoxTokenRequestParams();
            vivoxRequestParams.name = channelName;
            vivoxRequestParams.type = channelType;
            StartCoroutine(PostRequest<VivoxTokenRequestParams, VivoxTokenResponse>("VivoxService.Join", vivoxRequestParams, onVivoxLoginTokenReceived));
        }

        public void GetRequestMatchTicket(int mode, OnRequestCompleteDelegate<RequestMatchTicketResponse> onRequestMatchTicketReceived)
        {
            foreach (PingInfo site in m_PingSites)
            {
                // calculate packet loss here?
            }

            RequestMatchTicketParams requestMatchTicketParams = new RequestMatchTicketParams();
            requestMatchTicketParams.mode = mode;
            requestMatchTicketParams.QosResults = m_PingSites;
            StartCoroutine(PostRequest<RequestMatchTicketParams, RequestMatchTicketResponse>("MatchMakerService.RequestMatch", requestMatchTicketParams, onRequestMatchTicketReceived));
        }

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
                    if (response.connection != "")
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

        //Possibly add a delegate that way we can call invoke and this can be used to poll anything
        public IEnumerator PollMatch(string ticketId, string delegateToken, OnRequestCompleteDelegate<MatchmakerPollingResponse> onMatchmakerPollingComplete)
        {
            m_TimePolling = 0;
            m_MatchFound = false;
            while (!m_MatchFound && m_TimePolling < 300)
            {
                StartCoroutine(PollMatchmaker(ticketId, delegateToken, onMatchmakerPollingComplete));
                yield return new WaitForSeconds(5.0f);
                m_TimePolling += 5;
            }
        }

        public IEnumerator PollMatchmaker(string ticketId, string delegateToken, OnRequestCompleteDelegate<MatchmakerPollingResponse> onMatchmakerPollingComplete)
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
                onMatchmakerPollingComplete(response, false);
            }
            else
            {
                Debug.Log("Received: " + uwr.downloadHandler.text);

                response = JsonUtility.FromJson<MatchmakerPollingResponse>(uwr.downloadHandler.text);
                if (response.assignment != null && response.assignment.connection != null)
                {
                    // call something to open the connection (will need delegate)
                    m_MatchFound = true;
                    onMatchmakerPollingComplete(response, true);
                }
                if (response.assignment.error != "")
                {
                    // propogate out error
                    m_MatchFound = true;
                    onMatchmakerPollingComplete(response, true);
                }
            }
        }

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

        IEnumerator PostRequest<TRequest, TResponse>(string method, TRequest requestParams, OnRequestCompleteDelegate<TResponse> onRequestCompleteDelegate)
        {
            RequestArgs<TRequest> args = new RequestArgs<TRequest>();
            args.@params = requestParams;
            args.id = 1;
            args.jsonrpc = "2.0";
            args.method = method;

            string jsonData = JsonUtility.ToJson(args);
            Debug.Log($"JsonData: {jsonData}");

            UnityWebRequest uwr = UnityWebRequest.Post(m_LocalBackendUrl, "POST");
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

        public void PingSites(OnPingSitesCompleteDelegate onPingCompleteDelegate)
        {
            m_PingsCompleted = 0;
            //TODO: test with multiple ping sites. Does the invoke happen after all of them are actually done/updated?
            foreach (PingInfo site in m_PingSites)
            {
                StartCoroutine(StartPing(site.ipv4, onPingCompleteDelegate));
            }
        }

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

            if(m_PingsCompleted == m_PingSites.Length)
            {
                Debug.Log("ping sites has completed");
                m_PingsCompleted = 0;
                onPingCompleteDelegate.Invoke();
            }
        }

        void UpdatePingSiteLatency(string ip, int time)
        {
            PingInfo site = Array.Find<PingInfo>(m_PingSites, check => check.ipv4 == ip);
            site.latency = time;
        }

        void UpdatePingSitePacketLoss(string ip)
        {
            PingInfo site = Array.Find<PingInfo>(m_PingSites, check => check.ipv4 == ip);
            site.packetloss++;
        }

        // Not sure where to store authtoken, for now caching it in RPC and retireving it whenever there is a request
        public void SetAuthToken(string token)
        {
            m_AuthToken = token;
        }

        public string GetAuthToken()
        {
            return m_AuthToken;
        }

        public void SetPingSites(PingInfo[] pingSites)
        {
            m_PingSites = pingSites;
        }
    }
}
