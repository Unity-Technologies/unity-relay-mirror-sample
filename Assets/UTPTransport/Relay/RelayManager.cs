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
		// private RelayAdapter.RelayAdapter m_RelayAdapter; // TODO: this should be folded in with the UtpTransport package
		private UtpTransport m_UtpTransport;

		public string joinCode; // server
		public Allocation allocation; // server

		public JoinAllocation joinAllocation; // client

		/// <summary>
		/// A callback for when a Relay server is allocated and a join code is fetched.
		/// </summary>
		public Action<string> OnRelayServerAllocated;

		private void Awake()
		{
			Debug.Log("RelayManager initialized");

			m_UtpTransport = gameObject.GetComponentInParent<UtpTransport>();
		}

		public void GetAllocationFromJoinCode(string joinCode, Action callback)
		{
			StartCoroutine(GetAllocationFromJoinCodeTask(joinCode, callback));
		}

		private IEnumerator GetAllocationFromJoinCodeTask(string joinCode, Action callback)
		{
			Task<JoinAllocation> joinTask = Relay.Instance.JoinAllocationAsync(joinCode);
			while (!joinTask.IsCompleted)
			{
				yield return null;
			}

			if (joinTask.IsFaulted)
			{
				Debug.LogError("Join allocation request failed");
				yield break;
			}

			joinAllocation = joinTask.Result;
			callback?.Invoke();
		}

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
				UtpLog.Error("List regions request failed");
				yield break;
			}

			callback?.Invoke(regionsTask.Result);
		}

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
				UtpLog.Error("Create allocation request failed");
				yield break;
			}

			callback?.Invoke(allocationTask.Result);
		}

		private void OnAllocateRelayServer(Allocation inAllocation)
		{
			allocation = inAllocation;

			Debug.Log("Got allocation: " + allocation.AllocationId.ToString());
			StartCoroutine(GetJoinCodeTask(allocation.AllocationId, OnGetJoinCode));
		}

		private IEnumerator GetJoinCodeTask(Guid allocationId, Action<string> callback)
		{
			Task<string> joinCodeTask = Relay.Instance.GetJoinCodeAsync(allocationId);
			while (!joinCodeTask.IsCompleted)
			{
				yield return null;
			}

			if (joinCodeTask.IsFaulted)
			{
				Debug.LogError("Get join code failed"); // TODO: controlled logging
				yield break;
			}

			callback?.Invoke(joinCodeTask.Result);
		}

		private void OnGetJoinCode(string inJoinCode)
		{
			joinCode = inJoinCode;

			Debug.Log("Got join code: " + joinCode);

			OnRelayServerAllocated?.Invoke(joinCode);
		}
	}
}