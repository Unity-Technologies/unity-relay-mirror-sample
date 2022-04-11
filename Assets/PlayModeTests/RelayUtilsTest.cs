using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Utp;
using System.Collections.Generic;

namespace Utp
{
    public class RelayUtilsTest
    {

        #region HostRelayData

        /// <summary>
        /// Tests the HostRelayData call inside RelayUtils (UDP).
        /// </summary>
        [Test]
        public void HostRelayData_AllocationWithUDPEndpoints_ReturnsRelayServerData()
        {
            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            serverEndpoints.Add(new RelayServerEndpoint("udp", RelayServerEndpoint.NetworkOptions.Udp, false, false, "0.0.0.0", 00000));
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];

            Allocation allocation = new Allocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes
            );

            //Assert data against null/default data
            RelayServerData data = RelayUtils.HostRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);
            Assert.AreNotEqual(data, default(RelayServerData));
        }

        /// <summary>
        /// Tests the HostRelayData call inside RelayUtils (TCP).
        /// </summary>
        [Test]
        public void HostRelayData_AllocationWithTCPEndpoints_ReturnsRelayServerData()
        {
            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            serverEndpoints.Add(new RelayServerEndpoint("tcp", RelayServerEndpoint.NetworkOptions.Tcp, false, false, "0.0.0.0", 00000));
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];

            Allocation allocation = new Allocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes
            );

            //Assert data against null/default data
            RelayServerData data = RelayUtils.HostRelayData(allocation, RelayServerEndpoint.NetworkOptions.Tcp);
            Assert.AreNotEqual(data, default(RelayServerData));
        }

        /// <summary>
        /// Tests that null allocation data throws a null exception inside HostRelayData (UDP).
        /// </summary>
        [Test]
        public void HostRelayData_WithNullAllocationUDP_ThrowsNullReferenceException()
        {
            //Create null allocation & Assert null
            Allocation allocation = null;

            Assert.Throws<System.NullReferenceException>(() =>
            {
                RelayServerData data = RelayUtils.HostRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);
            });
        }

        /// <summary>
        /// Tests that null allocation data throws a null exception inside HostRelayData (TCP).
        /// </summary>
        [Test]
        public void HostRelayData_WithNullAllocationTCP_ThrowsNullReferenceException()
        {
            //Create null allocation & Assert null
            Allocation allocation = null;

            Assert.Throws<System.NullReferenceException>(() =>
            {
                RelayServerData data = RelayUtils.HostRelayData(allocation, RelayServerEndpoint.NetworkOptions.Tcp);
            });
        }

        /// <summary>
        /// Tests that a bad connection type in HostRelayData will throw an argument exception.
        /// </summary>
        [Test]
        public void HostRelayData_WithInvalidConnectionType_ThrowsArgumentException()
        {
            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            serverEndpoints.Add(new RelayServerEndpoint("udp", RelayServerEndpoint.NetworkOptions.Udp, false, false, "0.0.0.0", 00000));
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];

            Allocation allocation = new Allocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes
            );

            //Assert exception thrown
            Assert.Throws<System.ArgumentException>(() =>
            {
                RelayServerData data = RelayUtils.HostRelayData(allocation, (RelayServerEndpoint.NetworkOptions)5);
            });
        }

        /// <summary>
        /// Tests that the endpoints list inside HostRelayData's allocation must have entries.
        /// </summary>
        [Test]
        public void HostRelayData_WithEmptyEndpointsInAllocation_ThrowsArgumentException()
        {
            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];

            Allocation allocation = new Allocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes
            );

            //Assert exception thrown
            Assert.Throws<System.ArgumentException>(() =>
            {
                RelayServerData data = RelayUtils.HostRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);
            });
        }

        #endregion

        #region PlayerRelayData

        /// <summary>
        /// Tests the PlayerRelayData call inside RelayUtils (UDP).
        /// </summary>
        [UnityTest]
        public void PlayerRelayData_AllocationWithUDPEndpoints_ReturnsRelayServerData()
        {
            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            serverEndpoints.Add(new RelayServerEndpoint("udp", RelayServerEndpoint.NetworkOptions.Udp, false, false, "0.0.0.0", 00000));
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];
            byte[] hostConnectionData = new byte[16];

            JoinAllocation allocation = new JoinAllocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes,
                hostConnectionData
            );

            //Assert data against null/default data
            RelayServerData data = RelayUtils.PlayerRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);
            Assert.AreNotEqual(data, default(RelayServerData));
        }

        /// <summary>
        /// Tests the PlayerRelayData call inside RelayUtils (TCP).
        /// </summary>
        [UnityTest]
        public void PlayerRelayData_AllocationWithTCPEndpoints_ReturnsRelayServerData()
        {
            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            serverEndpoints.Add(new RelayServerEndpoint("tcp", RelayServerEndpoint.NetworkOptions.Tcp, false, false, "0.0.0.0", 00000));
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];
            byte[] hostConnectionData = new byte[16];

            JoinAllocation allocation = new JoinAllocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes,
                hostConnectionData
            );

            //Assert data against null/default data
            RelayServerData data = RelayUtils.PlayerRelayData(allocation, RelayServerEndpoint.NetworkOptions.Tcp);
            Assert.AreNotEqual(data, default(RelayServerData));
        }

        /// <summary>
        /// Tests that null allocation data throws a null exception inside PlayerRelayData (UDP).
        /// </summary>
        [Test]
        public void PlayerRelayData_WithNullAllocationUDP_ThrowsNullReferenceException()
        {
            //Create null allocation & Assert null
            JoinAllocation allocation = null;

            Assert.Throws<System.NullReferenceException>(() =>
            {
                RelayServerData data = RelayUtils.PlayerRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);
            });
        }

        /// <summary>
        /// Tests that null allocation data throws a null exception inside PlayerRelayData (TCP).
        /// </summary>
        [Test]
        public void PlayerRelayData_WithNullAllocationTCP_ThrowsNullReferenceException()
        {
            //Create null allocation & Assert null
            JoinAllocation allocation = null;

            Assert.Throws<System.NullReferenceException>(() =>
            {
                RelayServerData data = RelayUtils.PlayerRelayData(allocation, RelayServerEndpoint.NetworkOptions.Tcp);
            });
        }

        /// <summary>
        /// Tests that a bad connection type in HostRelayData will throw an argument exception.
        /// </summary>
        [Test]
        public void PlayerRelayData_WithInvalidConnectionType_ThrowsArgumentException()
        {
            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            serverEndpoints.Add(new RelayServerEndpoint("udp", RelayServerEndpoint.NetworkOptions.Udp, false, false, "0.0.0.0", 00000));
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];
            byte[] hostConnectionData = new byte[16];

            JoinAllocation allocation = new JoinAllocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes,
                hostConnectionData
            );

            //Assert exception thrown
            Assert.Throws<System.ArgumentException>(() =>
            {
                RelayServerData data = RelayUtils.PlayerRelayData(allocation, (RelayServerEndpoint.NetworkOptions)5);
            });
        }

        /// <summary>
        /// Tests that the endpoints list inside PlayerRelayData's allocation must have entries.
        /// </summary>
        [Test]
        public void PlayerRelayData_WithEmptyEndpointsInAllocation_ThrowsArgumentException()
        {
            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];
            byte[] hostConnectionData = new byte[16];

            JoinAllocation allocation = new JoinAllocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes,
                hostConnectionData
            );

            //Assert exception thrown
            Assert.Throws<System.ArgumentException>(() =>
            {
                RelayServerData data = RelayUtils.PlayerRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);
            });
        }

        #endregion

    }

}