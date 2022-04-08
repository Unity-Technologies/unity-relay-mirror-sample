using System;
using System.Collections.Generic;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Utp
{
    public class DummyRelayManager : MonoBehaviour, IRelayManager
    {
        public Allocation ServerAllocation { get; set; }
        public JoinAllocation JoinAllocation { get; set; }
        public Action<string, string> OnRelayServerAllocated { get; set; }

        public void AllocateRelayServer(int maxPlayers, string regionId)
        {
            if (regionId == "sample-region")
            {
                OnRelayServerAllocated?.Invoke("JNCDE", null);
            }
            else
            {
                OnRelayServerAllocated?.Invoke(null, "Invalid regionId");
            } // using expected values to simulate failures.
        }

        public void GetAllocationFromJoinCode(string joinCode, Action<string> callback)
        {
            if (joinCode == "JNCDE")
            {
                callback?.Invoke(null);
            }
            else
            {
                callback?.Invoke("Invalid joinCode");
            }
        }

        public void GetRelayRegions(Action<List<Region>> callback)
        {
            List<Region> RegionList = new List<Region>();
            Region dummyRegion = new Region("sample-region", "Sample Region");
            RegionList.Add(dummyRegion);
            callback(RegionList);
        }
    }
}