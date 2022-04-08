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

    #region HostRelayData

    /// <summary>
    /// Tests the HostRelayData call inside RelayUtils (UDP). Returns true if proper data was recieved.
    /// </summary>
    /// <returns>True, if call was successful.</returns>
    [UnityTest]
    public IEnumerator HostRelayData_MethodReturnsServerDataUDP_True()
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
    /// Tests the HostRelayData call inside RelayUtils (TCP). Returns true if proper data was recieved.
    /// </summary>
    /// <returns>True, if call was successful.</returns>
    [UnityTest]
    public IEnumerator HostRelayData_MethodReturnsServerDataTCP_True()
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
        serverEndpoints.Add(new RelayServerEndpoint("tcp", RelayServerEndpoint.NetworkOptions.Tcp, false, false, "0.0.0.0", 00000));

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
        RelayServerData data = RelayUtils.HostRelayData(allocation, RelayServerEndpoint.NetworkOptions.Tcp);

        //Assert check
        Assert.AreNotEqual(data, default(RelayServerData));
    }

    /// <summary>
    /// Tests that null allocation data throws a null exception inside HostRelayData (UDP).
    /// </summary>
    /// <returns>A null reference exception.</returns>
    [Test]
    public void HostRelayData_AllocationMustNotBeNullUDP_NullReferenceException()
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
    /// Tests that null allocation data throws a null exception inside HostRelayData (TCP).
    /// </summary>
    /// <returns>A null reference exception.</returns>
    [Test]
    public void HostRelayData_AllocationMustNotBeNullTCP_NullReferenceException()
    {
        //Create null allocation
        Allocation allocation = null;

        //Check for null exception
        Assert.Throws<System.NullReferenceException>(() =>
        {
            //Recieve relay server data
            RelayServerData data = RelayUtils.HostRelayData(allocation, RelayServerEndpoint.NetworkOptions.Tcp);
        });
    }

    /// <summary>
    /// Tests that a bad connection type in HostRelayData will throw an argument exception.
    /// </summary>
    /// <returns>An argument exception.</returns>
    [UnityTest]
    public IEnumerator HostRelayData_ConnectionTypeMustBeValid_ArgumentException()
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

        //Check for argument exception
        Assert.Throws<System.ArgumentException>(() =>
        {
            //Recieve relay server data
            RelayServerData data = RelayUtils.HostRelayData(allocation, (RelayServerEndpoint.NetworkOptions)5);
        });
    }

    /// <summary>
    /// Tests that the endpoints list inside HostRelayData's allocation must have entries.
    /// </summary>
    /// <returns>An argument exception.</returns>
    [UnityTest]
    public IEnumerator HostRelayData_AllocationMustHaveEndpoints_ArgumentException()
    {
        //Create new relay server
        RelayServer relayServer = new RelayServer("0.0.0.0", 0000);

        //Wait till next frame
        yield return new WaitForEndOfFrame();

        //Create dummy data to inject into temporary relay allocation

        //GUID
        System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");

        //Endpoints (empty)
        List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();

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

        //Check for argument exception
        Assert.Throws<System.ArgumentException>(() =>
        {
            //Recieve relay server data
            RelayServerData data = RelayUtils.HostRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);
        });
    }

    #endregion

    #region PlayerRelayData

    /// <summary>
    /// Tests the PlayerRelayData call inside RelayUtils (UDP). Returns true if proper data was recieved.
    /// </summary>
    /// <returns>True, if call was successful.</returns>
    [UnityTest]
    public IEnumerator PlayerRelayData_MethodReturnsServerDataUDP_True()
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
    /// Tests the PlayerRelayData call inside RelayUtils (TCP). Returns true if proper data was recieved.
    /// </summary>
    /// <returns>True, if call was successful.</returns>
    [UnityTest]
    public IEnumerator PlayerRelayData_MethodReturnsServerDataTCP_True()
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
        serverEndpoints.Add(new RelayServerEndpoint("tcp", RelayServerEndpoint.NetworkOptions.Tcp, false, false, "0.0.0.0", 00000));

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
        RelayServerData data = RelayUtils.PlayerRelayData(allocation, RelayServerEndpoint.NetworkOptions.Tcp);

        //Assert check
        Assert.AreNotEqual(data, default(RelayServerData));
    }

    /// <summary>
    /// Tests that null allocation data throws a null exception inside PlayerRelayData (UDP).
    /// </summary>
    /// <returns>A null reference exception.</returns>
    [Test]
    public void PlayerRelayData_AllocationMustNotBeNullUDP_NullReferenceException()
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

    /// <summary>
    /// Tests that null allocation data throws a null exception inside PlayerRelayData (TCP).
    /// </summary>
    /// <returns>A null reference exception.</returns>
    [Test]
    public void PlayerRelayData_AllocationMustNotBeNullTCP_NullReferenceException()
    {
        //Create null allocation
        JoinAllocation allocation = null;

        //Check for null exception
        Assert.Throws<System.NullReferenceException>(() =>
        {
            //Recieve relay server data
            RelayServerData data = RelayUtils.PlayerRelayData(allocation, RelayServerEndpoint.NetworkOptions.Tcp);
        });
    }

    /// <summary>
    /// Tests that a bad connection type in HostRelayData will throw an argument exception.
    /// </summary>
    /// <returns>An argument exception.</returns>
    [UnityTest]
    public IEnumerator PlayerRelayData_ConnectionTypeMustBeValid_ArgumentException()
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

        //Check for argument exception
        Assert.Throws<System.ArgumentException>(() =>
        {
            //Recieve relay server data
            RelayServerData data = RelayUtils.PlayerRelayData(allocation, (RelayServerEndpoint.NetworkOptions)5);
        });
    }

    /// <summary>
    /// Tests that the endpoints list inside PlayerRelayData's allocation must have entries.
    /// </summary>
    /// <returns>An argument exception.</returns>
    [UnityTest]
    public IEnumerator PlayerRelayData_AllocationMustHaveEndpoints_ArgumentException()
    {
        //Create new relay server
        RelayServer relayServer = new RelayServer("0.0.0.0", 0000);

        //Wait till next frame
        yield return new WaitForEndOfFrame();

        //Create dummy data to inject into temporary relay allocation

        //GUID
        System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");

        //Endpoints (empty)
        List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();

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

        //Check for argument exception
        Assert.Throws<System.ArgumentException>(() =>
        {
            //Recieve relay server data
            RelayServerData data = RelayUtils.PlayerRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);
        });
    }

    #endregion

}
