using System;
using System.Collections.Generic;
using Unity.Services.Relay.Models;

namespace Utp
{
    public interface IRelayManager
	{
		/// <summary>
		/// The allocation managed by a host who is running as a client and server.
		/// </summary>
		public Allocation ServerAllocation { get; set; }

		/// <summary>
		/// The allocation managed by a client who is connecting to a server.
		/// </summary>
		public JoinAllocation JoinAllocation { get; set; }

		/// <summary>
		/// A callback for when a Relay server is allocated and a join code is fetched.
		/// </summary>
		public Action<string, string> OnRelayServerAllocated { get; set; }

		/// <summary>
		/// Get a Relay Service JoinAllocation from a given joinCode.
		/// </summary>
		/// <param name="joinCode">The code to look up the joinAllocation for.</param>
		/// <param name="callback">A callback to invoke on success/error.</param>
		public void GetAllocationFromJoinCode(string joinCode, Action<string> callback);

        /// <summary>
        /// Get a list of Regions from the Relay Service.
        /// </summary>
        /// <param name="onSuccess">A callback to invoke when the list of regions is successfully retrieved.</param>
        /// <param name="onFailure">A callback to invoke when the list of regions is unsuccessfully retrieved.</param>
        public void GetRelayRegions(Action<List<Region>> onSuccess, Action onFailure);

		/// <summary>
		/// Allocate a Relay Server.
		/// </summary>
		/// <param name="maxPlayers">The max number of players that may connect to this server.</param>
		/// <param name="regionId">The region to allocate the server in. May be null.</param>
		public void AllocateRelayServer(int maxPlayers, string regionId);
	}
}