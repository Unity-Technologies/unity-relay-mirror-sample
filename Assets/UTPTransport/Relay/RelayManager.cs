using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Utp
{
	public class RelayManager : MonoBehaviour, IRelayManager
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
		/// The interface to the Relay services API.
		/// </summary>
		public IRelayServiceSDK RelayServiceSDK { get; set; } = new WrappedRelayServiceSDK();

		private void Awake()
		{
			UtpLog.Info("RelayManager initialized");
		}

		/// <summary>
		/// Get a Relay Service JoinAllocation from a given joinCode.
		/// </summary>
		/// <param name="joinCode">The code to look up the joinAllocation for.</param>
		/// <param name="callback">A callback to invoke on success/error.</param>
		public void GetAllocationFromJoinCode(string joinCode, Action<string> callback)
		{
			StartCoroutine(GetAllocationFromJoinCodeTask(joinCode, callback));
		}

		private IEnumerator GetAllocationFromJoinCodeTask(string joinCode, Action<string> callback)
		{
			Task<JoinAllocation> joinTask = RelayServiceSDK.JoinAllocationAsync(joinCode);
			while (!joinTask.IsCompleted)
			{
				yield return null;
			}

			if (joinTask.IsFaulted)
			{
				UtpLog.Error("Join allocation request failed");
				callback?.Invoke(joinTask.Exception.Message);

				yield break;
			}

			JoinAllocation = joinTask.Result;
			callback?.Invoke(null);
		}

		/// <summary>
		/// Get a list of Regions from the Relay Service.
		/// </summary>
		/// <param name="onSuccess">A callback to invoke when the list of regions is successfully retrieved.</param>
		/// <param name="onFailure">A callback to invoke when the list of regions is unsuccessfully retrieved.</param>
		public void GetRelayRegions(Action<List<Region>> onSuccess, Action onFailure)
		{
			StartCoroutine(GetRelayRegionsTask(onSuccess, onFailure));
		}

		private IEnumerator GetRelayRegionsTask(Action<List<Region>> onSuccess, Action onFailure)
		{
			Task<List<Region>> listRegions = RelayServiceSDK.ListRegionsAsync();

			while (!listRegions.IsCompleted)
			{
				yield return null;
			}

			if (listRegions.IsFaulted)
			{
				listRegions.Exception.Flatten().Handle((Exception err) =>
				{
					UtpLog.Error($"Unable to retrieve the list of Relay regions, encountered an error: {err.Message}.");
					return true;
				});

				onFailure?.Invoke();

				yield break;
			}

			onSuccess?.Invoke(listRegions.Result);
		}

        /// <summary>
        /// Allocate a Relay Server.
        /// </summary>
        /// <param name="maxPlayers">The max number of players that may connect to this server.</param>
        /// <param name="regionId">The region to allocate the server in. May be null.</param>
        /// <param name="onSuccess">A callback to invoke when the Relay server is successfully allocated.</param>
        /// <param name="onFailure">A callback to invoke when the Relay server is unsuccessfully allocated.</param>
        public void AllocateRelayServer(int maxPlayers, string regionId, Action<string> onSuccess, Action onFailure)
		{
			StartCoroutine(AllocateRelayServerTask(maxPlayers, regionId, onSuccess, onFailure));
		}

		private IEnumerator AllocateRelayServerTask(int maxPlayers, string regionId, Action<string> onSuccess, Action onFailure)
		{
            Task<Allocation> createAllocation = RelayServiceSDK.CreateAllocationAsync(maxPlayers, regionId);

            while (!createAllocation.IsCompleted)
            {
                yield return null;
            }

            if (createAllocation.IsFaulted)
            {
				createAllocation.Exception.Flatten().Handle((Exception err) =>
				{
					UtpLog.Error($"Unable to allocate Relay server, encountered an error creating a Relay allocation: {err.Message}.");
					return true;
				});

				onFailure?.Invoke();

                yield break;
            }

			ServerAllocation = createAllocation.Result;

            UtpLog.Verbose($"Received allocation: {ServerAllocation.AllocationId}");

			StartCoroutine(GetJoinCodeTask(onSuccess, onFailure));
        }

		private IEnumerator GetJoinCodeTask(Action<string> onSuccess, Action onFailure)
        {
            Task<string> getJoinCode = RelayServiceSDK.GetJoinCodeAsync(ServerAllocation.AllocationId);

            while (!getJoinCode.IsCompleted)
            {
                yield return null;
            }

            if (getJoinCode.IsFaulted)
            {
				getJoinCode.Exception.Flatten().Handle((Exception err) =>
                {
                    UtpLog.Error($"Unable to allocate Relay server, encountered an error retrieving the join code: {err.Message}.");
                    return true;
                });

                onFailure?.Invoke();

                yield break;
            }

			onSuccess?.Invoke(getJoinCode.Result);
        }
	}
}