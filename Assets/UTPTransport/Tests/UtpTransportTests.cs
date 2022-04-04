using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Utp;
using System.Threading.Tasks;
using System;

public class UtpTransportTests
{


    UtpTransport _Server;
    UtpTransport _Client;


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
    public void SetUp() {
        var ServerObj = new GameObject();
        var ClientObj = new GameObject();
        _Server = ServerObj.AddComponent<UtpTransport>();
        _Client = ClientObj.AddComponent<UtpTransport>();
    }
    [TearDown]
    public void TearDown() {
        _Client.ClientDisconnect();
        try
        {
            _Server.ServerStop();
        } catch (ObjectDisposedException) {
            Debug.Log("Server already disposed, ignoring in Teardown.");
        }
        GameObject.Destroy(_Server.gameObject);
        GameObject.Destroy(_Client.gameObject);
    }
    [Test]
    public void ServerActive_IsNotActive_False()
    {
        Assert.IsFalse(_Server.ServerActive(), "Server is running, but should not be.");
    }
    [Test]
    public void ServerActive_IsActive_True() {
        _Server.ServerStart();
        Assert.IsTrue(_Server.ServerActive(), "Server is not running, but should be.");
    }
    [Test]
    public void ServerStop_IsActive_False() {
        _Server.ServerStart();
        _Server.ServerStop();
        Assert.IsFalse(_Server.ServerActive(), "Server is running, but should not be.");
    }
    [Test]
    public void ServerGetClientAddress_InvalidAddress_EmptyString() {
        string clientAddress = _Server.ServerGetClientAddress(0);
        Assert.IsEmpty(clientAddress, "A client address was returned instead of an empty string.");
    }
    [Test]
    public void ClientConnected_NotConnected_False() {
        Assert.IsFalse(_Client.ClientConnected(), "Client is connected, but should not be.");
    }
    [UnityTest]
    public IEnumerator ClientConnected_IsConnected_True() {
        _Server.ServerStart();
        _Client.ClientConnect(_Server.ServerUri());
        yield return new WaitForConnectionOrTimeout(_Client, _Server, 30f);
        Assert.IsTrue(_Client.ClientConnected(), "Client is not connected, but should be.");
    }
}
