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
            // TODO: Do whatever is necessary to exercise the test.
        }

        public void GetAllocationFromJoinCode(string joinCode, Action<string> callback)
        {
            // TODO: Do whatever is necessary to exercise the test.
        }

        public void GetRelayRegions(Action<List<Region>> callback)
        {
            // TODO: Do whatever is necessary to exercise the test.
        }
    }
}