using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Utp;
using System.Collections.Generic;

public class RelayUtilsTest
{
    /// <summary>
    /// Tests the HostRelayData call inside RelayUtils. Returns true if proper data was recieved.
    /// </summary>
    /// <returns>True, if call was successful.</returns>
    [UnityTest]
    public IEnumerator HostRelayData_MethodReturnsServerData_True()
    {
        //Create new relay server
        RelayServer relayServer = new RelayServer("0.0.0.0", 0000);

        //Wait till next frame
        yield return new WaitForEndOfFrame();

        //Create dummy data to inject into temporary relay allocation

        //GUID
        System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");

        //Endpoints
        List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
        serverEndpoints.Add(new RelayServerEndpoint("udp", RelayServerEndpoint.NetworkOptions.Udp, false, false, "0.0.0.0", 00000));

        //Base64 Keys
        byte[] key = new byte[16];
        byte[] connectionData = new byte[16];
        byte[] allocationIdBytes = new byte[16];

        //Create allocation
        Allocation allocation = new Allocation(
            allocationId,
            serverEndpoints,
            relayServer,
            key,
            connectionData,
            allocationIdBytes
        );

        //Recieve relay server data
        RelayServerData data = RelayUtils.HostRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);

        //Assert check
        Assert.AreNotEqual(data, default(RelayServerData));
    }

    /// <summary>
    /// Tests that null allocation data throws a null exception inside HostRelayData.
    /// </summary>
    /// <returns>A null reference exception.</returns>
    [Test]
    public void HostRelayData_AllocationMustNotBeNull_NullReferenceException()
    {
        //Create null allocation
        Allocation allocation = null;

        //Check for null exception
        Assert.Throws<System.NullReferenceException>(() =>
        {
            //Recieve relay server data
            RelayServerData data = RelayUtils.HostRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);
        });
    }

    /// <summary>
    /// Tests the PlayerRelayData call inside RelayUtils. Returns true if proper data was recieved.
    /// </summary>
    /// <returns>True, if call was successful.</returns>
    [UnityTest]
    public IEnumerator PlayerRelayData_MethodReturnsServerData_True()
    {
        //Create new relay server
        RelayServer relayServer = new RelayServer("0.0.0.0", 0000);

        //Wait till next frame
        yield return new WaitForEndOfFrame();

        //Create dummy data to inject into temporary relay allocation

        //GUID
        System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");

        //Endpoints
        List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
        serverEndpoints.Add(new RelayServerEndpoint("udp", RelayServerEndpoint.NetworkOptions.Udp, false, false, "0.0.0.0", 00000));

        //Base64 Keys
        byte[] key = new byte[16];
        byte[] connectionData = new byte[16];
        byte[] allocationIdBytes = new byte[16];
        byte[] hostConnectionData = new byte[16];

        //Create join allocation
        JoinAllocation allocation = new JoinAllocation(
            allocationId,
            serverEndpoints,
            relayServer,
            key,
            connectionData,
            allocationIdBytes,
            hostConnectionData
        );

        //Recieve relay server data
        RelayServerData data = RelayUtils.PlayerRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);

        //Assert check
        Assert.AreNotEqual(data, default(RelayServerData));
    }

    /// <summary>
    /// Tests that null allocation data throws a null exception inside PlayerRelayData.
    /// </summary>
    /// <returns>A null reference exception.</returns>
    [Test]
    public void PlayerRelayData_AllocationMustNotBeNull_NullReferenceException()
    {
        //Create null allocation
        JoinAllocation allocation = null;

        //Check for null exception
        Assert.Throws<System.NullReferenceException>(() =>
        {
            //Recieve relay server data
            RelayServerData data = RelayUtils.PlayerRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);
        });
    }
}
