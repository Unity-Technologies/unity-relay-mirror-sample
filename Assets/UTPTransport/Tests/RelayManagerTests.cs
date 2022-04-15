using NUnit.Framework;
using System.Collections.Generic;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Utp
{
    public class RelayManagerTests
    {
        private UtpTransport _server;
        private UtpTransport _client;
        private IRelayManager _relayManager;
        private RelayNetworkManager _relayNetworkManager;

        [SetUp]
        public void SetUp()
        {
            var ServerObj = new GameObject();
            _relayManager = ServerObj.AddComponent<DummyRelayManager>();
            _relayNetworkManager = ServerObj.AddComponent<RelayNetworkManager>();
            _server = ServerObj.AddComponent<UtpTransport>();

            var ClientObj = new GameObject();
            _client = ClientObj.AddComponent<UtpTransport>();
        }

        [TearDown]
        public void TearDown()
        {
            _client.ClientDisconnect();
            GameObject.Destroy(_client.gameObject);

            _server.ServerStop();
            GameObject.Destroy(_server.gameObject);
        }

        [Test]
        public void Server_GetRelayRegions_RelayEnabled_NonEmptyList()
        {
            _server.useRelay = true;
            _server.GetRelayRegions(
                (List<Region> regions) =>
                {
                    Assert.IsNotEmpty(regions, "Region list was unexpectedly empty.");
                }
            );
        }

        [Test]
        public void Server_GetRelayRegions_RelayDisabled_EmptyList()
        {
            _server.GetRelayRegions(
                (List<Region> regions) =>
                {
                    Assert.IsEmpty(regions, "Region list was unexpectedly non-empty.");
                }
            );
        }

        [Test]
        public void Server_AllocateRelayServer_ValidRegion_ReturnsNullErrorAndValidJoinCode()
        {
            _server.AllocateRelayServer(5, "sample-region", (string joinCode, string error) =>
            {
                Assert.IsTrue(error == null, "An error was returned unexpectedly.");
                Assert.IsTrue(joinCode == "JNCDE", "The expected join code was not returned.");
            });
        }

        [Test]
        public void Server_AllocateRelayServer_InvalidRegion_ReturnsErrorAndNullJoinCode()
        {
            _server.AllocateRelayServer(5, "no-region", (string joinCode, string error) =>
            {
                Assert.IsTrue(error == "Invalid regionId", "The expected error was not returned.");
                Assert.IsNull(joinCode, "A join code was returned unexpectedly.");
            });
        }

        [Test]
        public void Server_GetAllocationFromJoinCode_NoError()
        {
            _relayManager.GetAllocationFromJoinCode("JNCDE", (error) =>
            {
                Assert.IsNull(error, "An error was returned unexpectedly.");
            });
        }

        [Test]
        public void Server_GetAllocationFromJoinCode_WithError()
        {
            _relayManager.GetAllocationFromJoinCode("BADCD", (error) =>
            {
                Assert.IsTrue(error == "Invalid joinCode", "The expected error was not returned.");
            });
        }
    }
}
