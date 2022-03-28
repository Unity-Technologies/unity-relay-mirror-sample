using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Utp
{
	public class RelayManager : MonoBehaviour
	{
		/// <summary>
		/// An instance of the UTP logger.
		/// </summary>
		public UtpLog logger;

		/// <summary>
		/// The allocation managed by a host who is running as a client and server.
		/// </summary>
		public Allocation serverAllocation;

		/// <summary>
		/// The allocation managed by a client who is connecting to a server.
		/// </summary>
		public JoinAllocation joinAllocation;

		/// <summary>
		/// A callback for when a Relay server is allocated and a join code is fetched.
		/// </summary>
		public Action<string, string> OnRelayServerAllocated;

		/// <summary>
		/// Instantiates a new relay manager instance.
		/// </summary>
		public RelayManager()
        {
			logger = new UtpLog("[RelayManager] ");
        }

		private void Awake()
		{
			logger.Info("Initialized!");
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
			Task<JoinAllocation> joinTask = Relay.Instance.JoinAllocationAsync(joinCode);
			while (!joinTask.IsCompleted)
			{
				yield return null;
			}

			if (joinTask.IsFaulted)
			{
				logger.Error("Join allocation request failed");
				callback?.Invoke(joinTask.Exception.Message);

				yield break;
			}

			joinAllocation = joinTask.Result;
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
			Task<List<Region>> regionsTask = Relay.Instance.ListRegionsAsync();
			while (!regionsTask.IsCompleted)
			{
				yield return null;
			}

			if (regionsTask.IsFaulted)
			{
				logger.Error("List regions request failed");
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
			Task<Allocation> allocationTask = Relay.Instance.CreateAllocationAsync(maxPlayers, regionId);
			while (!allocationTask.IsCompleted)
			{
				yield return null;
			}

			if (allocationTask.IsFaulted)
			{
				logger.Error("Create allocation request failed");
				OnRelayServerAllocated?.Invoke(null, allocationTask.Exception.Message);

				yield break;
			}

			callback?.Invoke(allocationTask.Result);
		}

		private void OnAllocateRelayServer(Allocation allocation)
		{
			serverAllocation = allocation;

			logger.Verbose("Got allocation: " + serverAllocation.AllocationId.ToString());
			StartCoroutine(GetJoinCodeTask(serverAllocation.AllocationId, OnRelayServerAllocated));
		}

		private IEnumerator GetJoinCodeTask(Guid allocationId, Action<string, string> callback)
		{
			Task<string> joinCodeTask = Relay.Instance.GetJoinCodeAsync(allocationId);
			while (!joinCodeTask.IsCompleted)
			{
				yield return null;
			}

			if (joinCodeTask.IsFaulted)
			{
				logger.Error("Get join code failed");
				callback?.Invoke(null, joinCodeTask.Exception.Message);

				yield break;
			}

			callback?.Invoke(joinCodeTask.Result, null);
		}

		/// <summary>
		/// Enables logging for this module.
		/// </summary>
		/// <param name="logLevel">The log level to set this logger to.</param>
		public void EnableLogging(LogLevel logLevel = LogLevel.Verbose)
		{
			logger.SetLogLevel(logLevel);
		}

		/// <summary>
		/// Disables logging for this module.
		/// </summary>
		public void DisableLogging()
		{
			logger.SetLogLevel(LogLevel.Off);
		}
	}
}