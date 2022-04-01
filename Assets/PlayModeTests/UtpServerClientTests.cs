using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Utp;

public class UtpServerClientTests
{
    private UtpServer _server;
    private UtpClient _client;

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

        private UtpClient _client = null;
        private UtpServer _server = null;

        public WaitForConnectionOrTimeout(UtpClient client, UtpServer server, float timeoutInSeconds)
        {
            _client = client;
            _server = server;
            _timeout = timeoutInSeconds;
        }

        public bool MoveNext()
        {
            _client.Tick();
            _server.Tick();

            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= _timeout)
            {
                Result = Status.TimedOut;
                return false;
            }
            else if (_client.IsConnected())
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
        _server = new UtpServer
        (
            (connectionId) => { },
            (connectionId, message) => { },
            (connectionId) => { },
            timeout: 1000
        );

        _client = new UtpClient(
            () => { },
            (message) => { },
            () => { },
            timeout: 1000
        );
    }

    [TearDown]
    public void TearDown()
    {
        _client.Disconnect();
        _server.Stop();
    }

    [Test]
    public void Server_IsActive_NotStarted_False()
    {
        Assert.IsFalse(_server.IsActive());
    }

    [Test]
    public void Server_IsActive_Started_True()
    {
        _server.Start(7777);
        Assert.IsTrue(_server.IsActive());
    }

    [UnityTest]
    public IEnumerator Client_IsConnected_NotConnected_False()
    {
        Assert.IsFalse(_client.IsConnected());
        yield return null;
    }

    [UnityTest]
    public IEnumerator Client_IsConnected_NoServer_False()
    {
        _client.Connect("localhost", 7777);
        Assert.IsFalse(_client.IsConnected());
        yield return null;
    }

    [UnityTest]
    public IEnumerator Client_IsConnected_WithServer_True()
    {
        _server.Start(7777);
        _client.Connect("localhost", 7777);

        yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);

        Assert.IsTrue(_client.IsConnected());
    }

    [Test]
    public void Server_GetClientAddress_NotConnected_EmptyString()
    {
        string clientAddress = _server.GetClientAddress(0);
        Assert.IsEmpty(clientAddress);
    }

    [UnityTest]
    public IEnumerator Server_GetClientAddress_Connected_NonEmptyString()
    {
        _server.Start(7777);
        _client.Connect("localhost", 7777);

        yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);

        int idOfFirstClient = 1;
        string clientAddress = _server.GetClientAddress(idOfFirstClient);
        Assert.IsNotEmpty(clientAddress);
    }
}
