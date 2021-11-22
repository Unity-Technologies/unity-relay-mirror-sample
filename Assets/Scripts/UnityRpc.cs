using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

namespace Rpc
{

    public delegate void OnPingSitesCompleteDelegate();

    public class UnityRpc : MonoBehaviour
    {
        private string m_AuthToken;

        //Replace UPID in the endpoint with the proper UPID once one is generated
        private string ticketEndpoint = "https://cloud.connected.unity3d.com/d98bfc46-6576-4059-b530-dc74eb4f1388/matchmaking/api/v1/tickets";

        string BackendUrl = "http://104.149.129.150:8080/rpc";
        string localBackendUrl = "http://172.31.231.55:8080/rpc";

        PingInfo[] m_PingSites;
        MultiplayProfile[] profiles;

        bool matchFound;
        bool matchCreated;
        int timePolling;
        int pingsCompleted;
        float pingTimeout = 0.05f;

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
                    StartCoroutine(PollUnity(response.uuid, allocateParams.fleet_id));
                }
                else
                {
                    Debug.Log("AllocateServer: failed allocating server");
                }
            };

            StartCoroutine(PostRequest<AllocateRequestParams, AllocateResponse>(m_AuthToken, "MultiplayService.Allocate", allocateParams, onAllocateComplete));
        }

        public void Login(string username, OnRequestCompleteDelegate<SignInResponse> onLoginCompleteDelegate)
        {
            SignInParams signInParams = new SignInParams();
            signInParams.username = username;
            StartCoroutine(PostRequest<SignInParams, SignInResponse>("", "AuthService.SignIn", signInParams, onLoginCompleteDelegate));
        }

        public void GetVivoxLoginToken(string user, string authToken, OnRequestCompleteDelegate<VivoxTokenResponse> onVivoxLoginTokenReceived)
        {
            VivoxTokenRequestParams vivoxRequestParams = new VivoxTokenRequestParams();
            vivoxRequestParams.user = user;
            StartCoroutine(PostRequest<VivoxTokenRequestParams, VivoxTokenResponse>(authToken, "VivoxService.Login", vivoxRequestParams, onVivoxLoginTokenReceived));
        }

        public void GetVivoxJoinToken(string channelName, string channelType, string authToken, OnRequestCompleteDelegate<VivoxTokenResponse> onVivoxLoginTokenReceived)
        {
            VivoxTokenRequestParams vivoxRequestParams = new VivoxTokenRequestParams();
            vivoxRequestParams.name = channelName;
            vivoxRequestParams.type = channelType;
            StartCoroutine(PostRequest<VivoxTokenRequestParams, VivoxTokenResponse>(authToken, "VivoxService.Join", vivoxRequestParams, onVivoxLoginTokenReceived));
        }

        public void GetRequestMatchTicket(int mode, string authToken, OnRequestCompleteDelegate<RequestMatchTicketResponse> onRequestMatchTicketReceived)
        {
            foreach (PingInfo site in m_PingSites)
            {
                // calculate packet loss here?
            }

            RequestMatchTicketParams requestMatchTicketParams = new RequestMatchTicketParams();
            requestMatchTicketParams.mode = mode;
            requestMatchTicketParams.QosResults = m_PingSites;
            StartCoroutine(PostRequest<RequestMatchTicketParams, RequestMatchTicketResponse>(authToken, "MatchMakerService.RequestMatch", requestMatchTicketParams, onRequestMatchTicketReceived));
        }

        public IEnumerator PollUnity(string uuid, string fleetId)
        {
            timePolling = 0;
            matchCreated = false;
            while (!matchCreated && timePolling < 300)
            {
                PollMultiplay(uuid, fleetId);
                yield return new WaitForSeconds(5.0f);
                timePolling += 5;
            }
        }

        public void PollMultiplay(string uuid, string fleetId)
        {
            PollMultiplayParams pollMultiplayParams = new PollMultiplayParams();
            pollMultiplayParams.uuid = uuid;
            pollMultiplayParams.fleetid = fleetId;

            OnRequestCompleteDelegate<PollMultiplayResponse> onMultiplayPollComplete = delegate (PollMultiplayResponse response, bool wasSuccessful)
            {
                if (!wasSuccessful || response.state == "Error" || response.status == "500")
                {
                    // TODO: handle error here
                    Debug.Log($"PollMultiplay: failed retrieving connection from multiplay");
                    matchCreated = true;
                }
                if(response.connection != "")
                {
                    Debug.Log($"PollMultiplay: successfully retrieved connetion from multiplay: {response.connection}");
                    string test = response.connection;
                    matchCreated = true;
                }
            };

            StartCoroutine(PostRequest<PollMultiplayParams, PollMultiplayResponse>(m_AuthToken, "MultiplayService.SingleAllocations", pollMultiplayParams, onMultiplayPollComplete));
        }

        //Possibly add a delegate that way we can call invoke and this can be used to poll anything
        public IEnumerator PollMatch(string ticketId, string delegateToken, OnRequestCompleteDelegate<MatchmakerPollingResponse> onMatchmakerPollingComplete)
        {
            timePolling = 0;
            matchFound = false;
            while (!matchFound && timePolling < 300)
            {
                StartCoroutine(PollMatchmaker(ticketId, delegateToken, onMatchmakerPollingComplete));
                yield return new WaitForSeconds(5.0f);
                timePolling += 5;
            }
        }

        public IEnumerator PollMatchmaker(string ticketId, string delegateToken, OnRequestCompleteDelegate<MatchmakerPollingResponse> onMatchmakerPollingComplete)
        {
            string url = ticketEndpoint + "?id=" + ticketId;

            UnityWebRequest uwr = UnityWebRequest.Get(url);
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Accept", "application/json");
            uwr.SetRequestHeader("Authorization", "BEARER " + delegateToken);
            

            //Send the request then wait here until it returns
            yield return uwr.SendWebRequest();

            MatchmakerPollingResponse response = new MatchmakerPollingResponse();

            if (uwr.isNetworkError)
            {
                Debug.Log("Error While Sending: " + uwr.error);
                matchFound = true;
                onMatchmakerPollingComplete(response, false);
            }
            else
            {
                Debug.Log("Received: " + uwr.downloadHandler.text);

                response = JsonUtility.FromJson<MatchmakerPollingResponse>(uwr.downloadHandler.text);
                if (response.assignment != null && response.assignment.connection != null)
                {
                    // call something to open the connection (will need delegate)
                    matchFound = true;
                    onMatchmakerPollingComplete(response, true);
                }
                if (response.assignment.error != "")
                {
                    // propogate out error
                    matchFound = true;
                    onMatchmakerPollingComplete(response, true);
                }
            }
        }

        public void GetEnvironment(string authToken)
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
            StartCoroutine(PostRequest<EnvironmentRequestParams, EnvironmentResponse>(authToken, "MultiplayService.Environment", environmentRequestParams, onEnvironmentReceived));
        }

        IEnumerator PostRequest<TRequest, TResponse>(string authToken, string method, TRequest requestParams, OnRequestCompleteDelegate<TResponse> onRequestCompleteDelegate)
        {
            RequestArgs<TRequest> args = new RequestArgs<TRequest>();
            args.@params = requestParams;
            args.id = 1;
            args.jsonrpc = "2.0";
            args.method = method;

            string jsonData = JsonUtility.ToJson(args);
            Debug.Log($"JsonData: {jsonData}");

            UnityWebRequest uwr = UnityWebRequest.Post(localBackendUrl, "POST");
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
            uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Accept", "application/json");
            if(authToken != "")
            {
                uwr.SetRequestHeader("Authorization", "BEARER " + authToken);
            }

            ResponseResult<TResponse> response = new ResponseResult<TResponse>();

            //Send the request then wait here until it returns
            yield return uwr.SendWebRequest();

            if (uwr.isNetworkError)
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
            pingsCompleted = 0;
            //TODO: test with multiple ping sites. Does the invoke happen after all of them are actually done/updated?
            foreach (PingInfo site in m_PingSites)
            {
                StartCoroutine(StartPing(site.ipv4, onPingCompleteDelegate));
            }
        }

        IEnumerator StartPing(string ip, OnPingSitesCompleteDelegate onPingCompleteDelegate)
        {
            WaitForSeconds f = new WaitForSeconds(pingTimeout);
            Ping p = new Ping(ip);
            while (!p.isDone)
            {
                yield return f;
            }
            PingFinished(p, onPingCompleteDelegate);
        }

        void PingFinished(Ping p, OnPingSitesCompleteDelegate onPingCompleteDelegate)
        {
            if(p.time >= pingTimeout)
            {
                UpdatePingSitePacketLoss(p.ip);
            }
            else
            {
                UpdatePingSiteLatency(p.ip, p.time);
            }

            pingsCompleted++;

            if(pingsCompleted == m_PingSites.Length)
            {
                Debug.Log("ping sites has completed");
                pingsCompleted = 0;
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
