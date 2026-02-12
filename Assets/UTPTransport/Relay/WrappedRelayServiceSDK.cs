using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Utp
{
    public class WrappedRelayServiceSDK : IRelayServiceSDK
    {
        public Task<Allocation> CreateAllocationAsync(int maxConnections, string region = null)
        {
            return RelayService.Instance.CreateAllocationAsync(maxConnections, region);
        }

        public Task<string> GetJoinCodeAsync(Guid allocationId)
        {
            return RelayService.Instance.GetJoinCodeAsync(allocationId);
        }

        public Task<JoinAllocation> JoinAllocationAsync(string joinCode)
        {
            return RelayService.Instance.JoinAllocationAsync(joinCode);
        }

        public Task<List<Region>> ListRegionsAsync()
        {
            return RelayService.Instance.ListRegionsAsync();
        }
    }
}