using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace Utp
{
    public class RelayManagerTests
    {
        private RelayManager _relayManager;

        [SetUp]
        public void SetUp()
        {
            var obj = new GameObject();
            _relayManager = obj.AddComponent<RelayManager>();
        }

        [TearDown]
        public void TearDown()
        {
            _relayManager.OnRelayServerAllocated = null;

            GameObject.Destroy(_relayManager.gameObject);
        }

        [Test]
        public void GetAllocationFromJoinCode_FaultedTask_LogsAnErrorAndReturnsAnErrorMessage()
        {
            _relayManager.RelayServiceSDK = new TaskAlwaysFaults();

            string errorMessage = null;
            string validJoinCode = "test";
            Action<string> onError = (err) => { errorMessage = err; };
            _relayManager.GetAllocationFromJoinCode(joinCode: validJoinCode, callback: onError);

            LogAssert.Expect(LogType.Error, "Join allocation request failed");
            Assert.That(_relayManager.JoinAllocation, Is.Null);
            Assert.That(errorMessage, Is.Not.Null);
            Assert.That(errorMessage, Is.Not.Empty);
        }

        [Test]
        public void GetAllocationFromJoinCode_CompletedTask_NullErrorAndNotNullAllocationId()
        {
            _relayManager.RelayServiceSDK = new TaskAlwaysCompletes();

            string errorMessage = null;
            Action<string> onError = (err) => { errorMessage = err; };
            _relayManager.GetAllocationFromJoinCode("test", onError);
            Assert.That(errorMessage, Is.Null);
            Assert.That(_relayManager.JoinAllocation.AllocationId, Is.Not.Null);
        }

        [Test]
        public void GetRelayRegions_FaultedTask_InvokesOnFailure()
        {
            _relayManager.RelayServiceSDK = new TaskAlwaysFaults();

            bool didInvokeOnSucess = false;
            bool didInvokeOnFailure = false;
            Action<List<Region>> onSuccess = (regions) => { didInvokeOnSucess = true; };
            Action onFailure = () => { didInvokeOnFailure = true; };
            _relayManager.GetRelayRegions(onSuccess: onSuccess, onFailure: onFailure);

            LogAssert.Expect(LogType.Error, new Regex(@"Encountered an error retrieving the list of Relay regions:"));
            Assert.That(didInvokeOnSucess, Is.False);
            Assert.That(didInvokeOnFailure, Is.True);
        }

        [Test]
        public void GetRelayRegions_CompletedTask_InvokesOnSuccess()
        {
            _relayManager.RelayServiceSDK = new TaskAlwaysCompletes();

            bool didInvokeOnSucess = false;
            bool didInvokeOnFailure = false;
            Action<List<Region>> onSuccess = (regions) => { didInvokeOnSucess = true; };
            Action onFailure = () => { didInvokeOnFailure = true; };
            _relayManager.GetRelayRegions(onSuccess: onSuccess, onFailure: onFailure);

            Assert.That(didInvokeOnSucess, Is.True);
            Assert.That(didInvokeOnFailure, Is.False);
        }

        [Test]
        public void AllocateRelayServer_FaultedTask_LogsAnErrorAndInvokesOnRelayServerAllocated()
        {
            _relayManager.RelayServiceSDK = new TaskAlwaysFaults();

            string joinCode = null;
            string errorMessage = null;
            Action<string, string> onRelayServerAllocated = (code, error) => { joinCode = code; errorMessage = error; };
            _relayManager.OnRelayServerAllocated += onRelayServerAllocated;

            int validMaxPlayers = 8;
            string validRegionId = "test";
            _relayManager.AllocateRelayServer(maxPlayers: validMaxPlayers, regionId: validRegionId);

            LogAssert.Expect(LogType.Error, "Create allocation request failed");
            Assert.That(_relayManager.ServerAllocation, Is.Null);
            Assert.That(joinCode, Is.Null);
            Assert.That(errorMessage, Is.Not.Null);
        }

        [Test]
        public void AllocateRelayServer_CompletedTask_ServerAllocationNoErrorAndInvokesOnRelayServerAllocated()
        {
            _relayManager.RelayServiceSDK = new TaskAlwaysCompletes();

            string joinCode = null;
            string errorMessage = null;
            Action<string, string> onRelayServerAllocated = (code, error) => { joinCode = code; errorMessage = error; };
            _relayManager.OnRelayServerAllocated += onRelayServerAllocated;

            int validMaxPlayers = 8;
            string validRegionId = "test";
            _relayManager.AllocateRelayServer(maxPlayers: validMaxPlayers, regionId: validRegionId);

            Assert.That(_relayManager.ServerAllocation, Is.Not.Null);
            Assert.That(joinCode, Is.Not.Null);
            Assert.That(errorMessage, Is.Null);
        }

        private class TaskAlwaysCompletes : IRelayServiceSDK
        {
            public Task<Allocation> CreateAllocationAsync(int maxConnections, string region = null)
            {
                byte[] resultKey = new byte[4];
                byte[] resultConnectionData = new byte[4];
                byte[] resultAllocationIdBytes = new byte[4];
                Guid resultAllocationId = new Guid();
                List<RelayServerEndpoint> resultEndpointList = new List<RelayServerEndpoint>();
                string localHostIp = "127.0.0.1";
                ushort samplePort = 12345;
                RelayServer resultRelayServer = new RelayServer(ipV4: localHostIp, port: samplePort);
                return Task.FromResult<Allocation>(
                    result: new Allocation(
                        allocationId: resultAllocationId,
                        serverEndpoints: resultEndpointList,
                        relayServer: resultRelayServer,
                        key: resultKey,
                        connectionData: resultConnectionData,
                        allocationIdBytes: resultAllocationIdBytes,
                        region: region
                    )
                );
            }

            public Task<string> GetJoinCodeAsync(Guid allocationId)
            {
                string validJoinCode = "test";
                return Task.FromResult<string>(validJoinCode);
            }

            public Task<JoinAllocation> JoinAllocationAsync(string joinCode)
            {
                Guid joinAllocationAllocationId = new Guid();
                List<RelayServerEndpoint> joinAllocationEndpointList = new List<RelayServerEndpoint>();
                string localHostIp = "127.0.0.1";
                ushort samplePort = 12345;
                byte[] joinAllocationKey = new byte[4];
                byte[] joinAllocationHostConnectionData = new byte[4];
                byte[] joinAllocationConnectionData = new byte[4];
                string joinAllocationRegion = string.Empty;
                byte[] joinAllocationAllocationIdBytes = new byte[4];
                RelayServer joinAllocationRelayServer = new RelayServer(ipV4: localHostIp, port: samplePort);
                return Task.FromResult<JoinAllocation>(
                    result: new JoinAllocation(
                        allocationId: joinAllocationAllocationId,
                        serverEndpoints: joinAllocationEndpointList,
                        relayServer: joinAllocationRelayServer,
                        key: joinAllocationKey,
                        hostConnectionData: joinAllocationHostConnectionData,
                        connectionData: joinAllocationConnectionData,
                        region: joinAllocationRegion,
                        allocationIdBytes: joinAllocationAllocationIdBytes
                    )
                );
            }

            public Task<List<Region>> ListRegionsAsync()
            {
                List<Region> regionList = new List<Region>();
                Region validRegion = new Region(id: "valid-region", description: "test");
                regionList.Add(validRegion);
                return Task.FromResult<List<Region>>(
                    result: regionList
                );
            }
        }

        private class TaskAlwaysFaults : IRelayServiceSDK
        {
            public Task<Allocation> CreateAllocationAsync(int maxConnections, string region = null)
            {
                return Task.FromException<Allocation>(new Exception("Task faulted!"));
            }

            public Task<string> GetJoinCodeAsync(Guid allocationId)
            {
                return Task.FromException<string>(new Exception("Task faulted!"));
            }

            public Task<JoinAllocation> JoinAllocationAsync(string joinCode)
            {
                return Task.FromException<JoinAllocation>(new Exception("Task faulted!"));
            }

            public Task<List<Region>> ListRegionsAsync()
            {
                return Task.FromException<List<Region>>(new Exception("Task faulted!"));
            }
        }
    }
}
