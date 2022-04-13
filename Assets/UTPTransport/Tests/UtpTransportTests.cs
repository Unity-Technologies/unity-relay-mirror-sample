using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Services.Relay.Models;
using Utp;

public class UtpTransportTests
{


    UtpTransport _Server;
    UtpTransport _Client;
    IRelayManager _RelayManager;


    private class WaitForConnectionOrTimeout : IEnumerator
    {
        public enum Status
        {
            Undetermined,
            ClientConnected,
            TimedOut,
        }

        public Status Result { get; private set; } = Status.Undetermined;

        public object Current => null;

        private float _elapsedTime = 0f;
        private float _timeout = 0f;

        private UtpTransport _client = null;
        private UtpTransport _server = null;

        public WaitForConnectionOrTimeout(UtpTransport client, UtpTransport server, float timeoutInSeconds)
        {
            _client = client;
            _server = server;
            _timeout = timeoutInSeconds;
        }

        public bool MoveNext()
        {
            _client.ClientEarlyUpdate();
            _server.ServerEarlyUpdate();

            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= _timeout)
            {
                Result = Status.TimedOut;
                return false;
            }
            else if (_client.ClientConnected())
            {
                Result = Status.ClientConnected;
                return false;
            }

            return true;
        }

        public void Reset()
        {
            _elapsedTime = 0f;
            _timeout = 0f;
            Result = Status.Undetermined;
        }
    }
    [SetUp]
    public void SetUp()
    {
        var ServerObj = new GameObject();
        _RelayManager = ServerObj.AddComponent<DummyRelayManager>();
        _Server = ServerObj.AddComponent<UtpTransport>();

        var ClientObj = new GameObject();
        _Client = ClientObj.AddComponent<UtpTransport>();
    }
    [TearDown]
    public void TearDown()
    {
        _Client.ClientDisconnect();
        GameObject.Destroy(_Client.gameObject);

        _Server.ServerStop();
        GameObject.Destroy(_Server.gameObject);
    }
    [Test]
    public void ServerActive_IsNotActive_False()
    {
        Assert.IsFalse(_Server.ServerActive(), "Server is running, but should not be.");
    }
    [Test]
    public void ServerActive_IsActive_True()
    {
        _Server.ServerStart();
        Assert.IsTrue(_Server.ServerActive(), "Server is not running, but should be.");
    }
    [Test]
    public void ServerStop_IsActive_False()
    {
        _Server.ServerStart();
        _Server.ServerStop();
        Assert.IsFalse(_Server.ServerActive(), "Server is running, but should not be.");
    }
    [Test]
    public void ServerGetClientAddress_InvalidAddress_EmptyString()
    {
        string clientAddress = _Server.ServerGetClientAddress(0);
        Assert.IsEmpty(clientAddress, "A client address was returned instead of an empty string.");
    }
    [UnityTest]
    public IEnumerator ServerGetClientAddress_ClientConnected_NonEmptyString()
    {
        _Server.ServerStart();
        _Client.ClientConnect(_Server.ServerUri());
        yield return new WaitForConnectionOrTimeout(_Client, _Server, 30f);
        int idOfFirstClient = 1;
        string clientAddress = _Server.ServerGetClientAddress(idOfFirstClient);
        Assert.IsNotEmpty(clientAddress, "A client address was not returned, connection possibly timed out..");
    }
    [Test]
    public void ClientConnected_NotConnected_False()
    {
        Assert.IsFalse(_Client.ClientConnected(), "Client is connected, but should not be.");
    }
    [UnityTest]
    public IEnumerator ClientConnected_IsConnected_True()
    {
        _Server.ServerStart();
        _Client.ClientConnect(_Server.ServerUri());
        yield return new WaitForConnectionOrTimeout(_Client, _Server, 30f);
        Assert.IsTrue(_Client.ClientConnected(), "Client is not connected, but should be.");
    }
    [Test]
    public void Server_GetRelayRegions_NonEmptyList()
    {
        _Server.GetRelayRegions(
            (List<Region> regions) =>
            {
                Assert.IsTrue(regions.Count > 0, "Region list was unexpectedly empty.");
            }
        );
    }
    [Test]
    public IEnumerator Server_AllocateRelayServer_NonEmptyJoinCode()
    {
        _Server.AllocateRelayServer(5, "sample-region", (string joinCode, string error) =>
        {
            Assert.IsTrue(error == null, "An error was returned unexpectedly.");
            Assert.IsTrue(joinCode == "JNCDE", "The expected join code was not returned.");
        });
    }
    [Test]
    public IEnumerator Server_AllocateRelayServer_EmptyJoinCode()
    {
        _Server.AllocateRelayServer(5, "no-region", (string joinCode, string error) =>
        {
            Assert.IsTrue(error == "Invalid regionId", "The expected error was not returned.");
            Assert.IsTrue(joinCode == null, "A join code was returned unexpectedly.");
        });
    }
    [Test]
    public void Server_GetAllocationFromJoinCode_NoError()
    {
        _RelayManager.GetAllocationFromJoinCode("JNCDE", (error) =>
        {
            Assert.IsNull(error, "An error was returned unexpectedly.");
        });
    }
    [Test]
    public void Server_GetAllocationFromJoinCode_WithError()
    {
        _RelayManager.GetAllocationFromJoinCode("BADCD", (error) =>
        {
            Assert.IsTrue(error == "Invalid joinCode", "The expected error was not returned.");
        });
    }
}
