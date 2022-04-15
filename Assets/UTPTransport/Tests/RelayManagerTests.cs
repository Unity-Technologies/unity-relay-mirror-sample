using NUnit.Framework;
using System;
using System.Collections.Generic;
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
        public void GetAllocationFromJoinCode_CompletedTask_TODO()
        {
            _relayManager.RelayServiceSDK = new TaskAlwaysCompletes();

            // TODO: Assert that RelayManager.JoinAllocation is populated and that callback is invoked with null error message.
        }

        [Test]
        public void GetRelayRegions_FaultedTask_LogsAnErrorAndReturnsAnEmptyList()
        {
            _relayManager.RelayServiceSDK = new TaskAlwaysFaults();

            List<Region> listOfRegions = null;
            Action<List<Region>> onRegionsRetrieved = (regions) => { listOfRegions = regions; };
            _relayManager.GetRelayRegions(callback: onRegionsRetrieved);

            LogAssert.Expect(LogType.Error, "List regions request failed");
            Assert.That(listOfRegions, Is.Not.Null);
            Assert.That(listOfRegions, Is.Empty);
        }

        [Test]
        public void GetRelayRegions_CompletedTask_TODO()
        {
            _relayManager.RelayServiceSDK = new TaskAlwaysCompletes();

            // TODO: Assert that callback is invoked with non-empty region list.
        }

        [Test]
        public void AllocateRelayServer_FaultedTask_LogsAnErrorAndInvokesOnRelayServerAllocated()
        {
            _relayManager.RelayServiceSDK = new TaskAlwaysFaults();

            string joinCode = null;
            string errorMessage = null;
            Action<string, string> onRelayServerAllocated = (code, error) => { joinCode = code;  errorMessage = error; };
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
        public void AllocateRelayServer_CompletedTask_TODO()
        {
            _relayManager.RelayServiceSDK = new TaskAlwaysCompletes();

            // TODO: Assert that RelayManager.ServerAllocation is populated and that OnRelayServerAllocated is invoked with valid join code and null error.
        }

        private class TaskAlwaysCompletes : IRelayServiceSDK
        {
            public Task<Allocation> CreateAllocationAsync(int maxConnections, string region = null)
            {
                // TODO: Replace with Task.FromResult()
                throw new NotImplementedException();
            }

            public Task<string> GetJoinCodeAsync(Guid allocationId)
            {
                // TODO: Replace with Task.FromResult()
                throw new NotImplementedException();
            }

            public Task<JoinAllocation> JoinAllocationAsync(string joinCode)
            {
                // TODO: Replace with Task.FromResult()
                throw new NotImplementedException();
            }

            public Task<List<Region>> ListRegionsAsync()
            {
                // TODO: Replace with Task.FromResult()
                throw new NotImplementedException();
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
