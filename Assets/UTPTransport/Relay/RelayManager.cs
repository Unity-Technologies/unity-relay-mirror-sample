using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Unity.Networking.Transport.Relay;
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

		public Action<string> OnRelayServerAllocated;
		public Action OnTransportConfiguredCallback;

		private void Awake()
		{
			Debug.Log("RelayManager initialized");

			// m_RelayAdapter = new RelayAdapter.RelayAdapter();
			m_UtpTransport = gameObject.GetComponentInParent<UtpTransport>();
		}

		public void GetAllocationFromJoinCode(string inJoinCode)
		{
			StartCoroutine(GetAllocationFromJoinCode(inJoinCode, OnAllocationReceived));
		}

		private IEnumerator GetAllocationFromJoinCode(string joinCode, Action<JoinAllocation> callback)
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

			callback?.Invoke(joinTask.Result);
		}

		private void OnAllocationReceived(JoinAllocation inJoinAllocation)
		{
			joinAllocation = inJoinAllocation;
			OnTransportConfiguredCallback?.Invoke();
		}

		public void AllocateRelayServer()
		{
			StartCoroutine(GetRegionList(OnGetRegionList));
		}

		private IEnumerator GetRegionList(Action<List<Region>> callback)
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

		private void OnGetRegionList(List<Region> regionList)
		{
			if (regionList.Count > 0)
			{
				bool foundRegion = false;

				for (int i = 0; i < regionList.Count; i++)
				{
					Region region = regionList[i];

					// For example purposes, always try to use us-east4
					if (region.Id == "us-east4")
					{
						foundRegion = true;
						Debug.Log("Found region. ID: " + region.Id + ", Name: " + region.Description);

						int maxPlayers = 8;
						StartCoroutine(AllocateRelayServer(maxPlayers, region.Id, OnAllocateRelayServer));
					}
				}

				if (!foundRegion)
				{
					Debug.LogWarning("Did not find specified region, not allocating a server");
				}
			}
			else
			{
				Debug.LogWarning("No regions received");
			}
		}

		private IEnumerator AllocateRelayServer(int maxPlayers, string region, Action<Allocation> callback)
		{
			Task<Allocation> allocationTask = Relay.Instance.CreateAllocationAsync(maxPlayers, region);
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
			StartCoroutine(GetJoinCode(allocation.AllocationId, OnGetJoinCode));
		}

		private IEnumerator GetJoinCode(Guid allocationId, Action<string> callback)
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