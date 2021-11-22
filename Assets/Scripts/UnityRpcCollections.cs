using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Rpc
{
    public delegate void OnRequestCompleteDelegate<T>(T responseArgs, bool wasSuccessful);
    [Serializable]
    public class RequestArgs<T>
    {
        // @ is an escape key to use keywords as a variable
        public T @params;
        public string jsonrpc;
        public string method;
        public int id; 
    }
    [Serializable]
    public class ResponseResult<T>
    {
        public T result;
    }

    [Serializable]
    public class SignInParams
    {
        public string username;
    }
    [Serializable]
    public class SignInResponse
    {
        public string username;
        public string token;
        public string userid;
        public PingInfo[] pingsites;
    }

    [Serializable]
    public class VivoxTokenRequestParams
    {
        public string user; // username - for login
        public string name; // Channel name - for join
        public string type; // channel type - for join
    }
    [Serializable]
    public class VivoxTokenResponse
    {
        public string token;
        public string uri;
        public string endpoint;
    }

    [Serializable]
    public class RequestMatchTicketParams
    {
        public PingInfo[] QosResults; // List of regions and their ping info
        public int mode; // Game Mode
    }
    [Serializable]
    public class RequestMatchTicketResponse
    {
        public string id;
        public string token;
        public string error;
    }

    [Serializable]
    public class EnvironmentRequestParams
    {
    }
    [Serializable]
    public class EnvironmentResponse
    {
        public MultiplayProfile[] profiles;
        public MultiplayProfile[] fleets; 
        public string status;
    }

    [Serializable]
    public class AllocateRequestParams
    {
        public string fleet_id;
        public string region_id;
        public string profile_id;
    }
    [Serializable]
    public class AllocateResponse
    {
        public string uuid;
    }

    [Serializable]
    public class PollMultiplayParams
    {
        public string uuid;
        public string fleetid;
    }
    [Serializable]
    public class PollMultiplayResponse
    {
        public string connection;
        public string status;
        public string state;
    }

    //NOTE: variable names must EXACTLY match the case for JSON tags otherwise they won't deserialize properly
    // Possibly move the stuctures below. For now organizing them in here to be organized later.

    [Serializable]
    public class PingInfo
    {
        public string regionid; 
        public string ipv4; 
        public string port;
        public double packetloss;
        public int latency;
    }

    [Serializable]
    public class MatchmakerPollingResponse
    {
        public MatchmakerAsssignment assignment;
    }

    [Serializable]
    public class MatchmakerAsssignment
    {
        public string connection;
        public string error;
        public string properties;
        public string matchProperties;
    }

    [Serializable]
    public class MultiplayProfile
    {
        public string id; // Multiplay profile id
        public string label; // label for the profile id
    }
}
