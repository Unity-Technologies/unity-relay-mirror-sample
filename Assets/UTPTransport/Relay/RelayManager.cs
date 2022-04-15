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
		/// <param name="callback">A callback to invoke on success/error.</param>
		public void GetRelayRegions(Action<List<Region>> callback)
		{
			StartCoroutine(GetRelayRegionsTask(callback));
		}

		private IEnumerator GetRelayRegionsTask(Action<List<Region>> callback)
		{
			Task<List<Region>> regionsTask = RelayServiceSDK.ListRegionsAsync();
			while (!regionsTask.IsCompleted)
			{
				yield return null;
			}

			if (regionsTask.IsFaulted)
			{
				UtpLog.Error("List regions request failed");
				callback?.Invoke(new List<Region>());
				yield break;
			}

			callback?.Invoke(regionsTask.Result);
		}

		/// <summary>
		/// Allocate a Relay Server.
		/// </summary>
		/// <param name="maxPlayers">The max number of players that may connect to this server.</param>
		/// <param name="regionId">The region to allocate the server in. May be null.</param>
		public void AllocateRelayServer(int maxPlayers, string regionId)
		{
			StartCoroutine(AllocateRelayServerTask(maxPlayers, regionId, OnAllocateRelayServer));
		}

		private IEnumerator AllocateRelayServerTask(int maxPlayers, string regionId, Action<Allocation> callback)
		{
			Task<Allocation> allocationTask = RelayServiceSDK.CreateAllocationAsync(maxPlayers, regionId);
			while (!allocationTask.IsCompleted)
			{
				yield return null;
			}

			if (allocationTask.IsFaulted)
			{
				UtpLog.Error("Create allocation request failed");
				OnRelayServerAllocated?.Invoke(null, allocationTask.Exception.Message);

				yield break;
			}

			callback?.Invoke(allocationTask.Result);
		}

		private void OnAllocateRelayServer(Allocation allocation)
		{
			ServerAllocation = allocation;

			UtpLog.Verbose("Got allocation: " + ServerAllocation.AllocationId.ToString());
			StartCoroutine(GetJoinCodeTask(ServerAllocation.AllocationId, OnRelayServerAllocated));
		}

		private IEnumerator GetJoinCodeTask(Guid allocationId, Action<string, string> callback)
		{
			Task<string> joinCodeTask = RelayServiceSDK.GetJoinCodeAsync(allocationId);
			while (!joinCodeTask.IsCompleted)
			{
				yield return null;
			}

			if (joinCodeTask.IsFaulted)
			{
				UtpLog.Error("Get join code failed");
				callback?.Invoke(null, joinCodeTask.Exception.Message);

				yield break;
			}

			callback?.Invoke(joinCodeTask.Result, null);
		}
	}
}