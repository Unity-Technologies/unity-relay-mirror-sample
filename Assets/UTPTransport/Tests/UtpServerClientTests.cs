using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Utp;

public class UtpServerClientTests
{
    private UtpServer _server;
    private UtpClient _client;
    private bool ServerOnConnectedCalled;
    private bool ServerOnDisconnectedCalled;
    private bool ServerOnReceivedDataCalled;
    private bool ClientOnConnectedCalled;
    private bool ClientOnDisconnectedCalled;
    private bool ClientOnReceivedDataCalled;

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
    private class WaitForDisconnectOrTimeout : IEnumerator
    {
        public enum Status
        {
            Undetermined,
            ClientDisconnected,
            TimedOut,
        }

        public Status Result { get; private set; } = Status.Undetermined;

        public object Current => null;

        private float _elapsedTime = 0f;
        private float _timeout = 0f;

        private UtpClient _client = null;
        private UtpServer _server = null;

        public WaitForDisconnectOrTimeout(UtpClient client, UtpServer server, float timeoutInSeconds)
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
            else if (!_client.IsConnected())
            {
                Result = Status.ClientDisconnected;
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
            (connectionId) => { ServerOnConnectedCalled = true; },
            (connectionId, message) => { ServerOnReceivedDataCalled = true; },
            (connectionId) => { ServerOnDisconnectedCalled = true; },
            timeout: 1000
        );

        _client = new UtpClient(
            () => { ClientOnConnectedCalled = true; },
            (message) => { ClientOnReceivedDataCalled = true; },
            () => { ClientOnDisconnectedCalled = true; },
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
        Assert.IsFalse(_server.IsActive(), "Server is active without being started.");
    }

    [Test]
    public void Server_IsActive_Started_True()
    {
        _server.Start(7777);
        Assert.IsTrue(_server.IsActive(), "Server did not start and is not active.");
    }

    [UnityTest]
    public IEnumerator Client_IsConnected_NotConnected_False()
    {
        Assert.IsFalse(_client.IsConnected(), "Client is connected without calling Connect().");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Client_IsConnected_NoServer_False()
    {
        _client.Connect("localhost", 7777);
        Assert.IsFalse(_client.IsConnected(), "Client is connected without server starting.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Client_IsConnected_WithServer_True()
    {
        _server.Start(7777);
        _client.Connect("localhost", 7777);

        yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);

        Assert.IsTrue(_client.IsConnected(), "Client was not able to connect with server.");
    }

    [Test]
    public void Server_GetClientAddress_NotConnected_EmptyString()
    {
        string clientAddress = _server.GetClientAddress(0);
        Assert.IsEmpty(clientAddress, "Client address was not empty.");
    }

    [UnityTest]
    public IEnumerator Server_GetClientAddress_Connected_NonEmptyString()
    {
        _server.Start(7777);
        _client.Connect("localhost", 7777);

        yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);

        int idOfFirstClient = 1;
        string clientAddress = _server.GetClientAddress(idOfFirstClient);
        Assert.IsNotEmpty(clientAddress, "Client address was empty, indicating the client is probably not connected.");
    }
    [UnityTest]
    public IEnumerator Server_Disconnect_NoClient_Warning()
    {
        _server.Disconnect(1);
        LogAssert.Expect(LogType.Warning, "Connection not found: 1");
        yield return null;
    }
    [UnityTest]
    public IEnumerator Server_Disconnect_ClientConnected_Success()
    {
        _server.Start(7777);
        _client.Connect("localhost", 7777);
        yield return new WaitForConnectionOrTimeout(_client, _server, 30f);
        Assert.IsTrue(_client.IsConnected(), "Client did not connect to server.");
        int idOfFirstClient = 1;
        _server.Disconnect(idOfFirstClient);
        yield return new WaitForDisconnectOrTimeout(_client, _server, 10f);
        Assert.IsFalse(_client.IsConnected(), "Client was not successfully disconnected from server");
    }
    [UnityTest]
    public IEnumerator Server_FindConnection_NoClient_DefaultConnection()
    {
        Unity.Networking.Transport.NetworkConnection FoundConnection = _server.FindConnection(1);
        Assert.IsTrue(FoundConnection == default(Unity.Networking.Transport.NetworkConnection), "A connection was found when no client was connected.");
        yield return null;
    }
    [UnityTest]
    public IEnumerator Server_FindConnection_NoClient_NonDefaultConnection()
    {
        _server.Start(7777);
        _client.Connect("localhost", 7777);
        yield return new WaitForConnectionOrTimeout(_client, _server, 30f);
        Unity.Networking.Transport.NetworkConnection FoundConnection = _server.FindConnection(1);
        Assert.IsFalse(FoundConnection == default(Unity.Networking.Transport.NetworkConnection), "No connection found.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Server_Client_Callbacks_Called()
    {
        _server.Start(7777);
        _client.Connect("localhost", 7777);
        Debug.Log($"Server Connected: {ServerOnConnectedCalled}");
        Debug.Log($"Server Disconnected: {ServerOnDisconnectedCalled}");
        Debug.Log($"Server Received Data: {ServerOnReceivedDataCalled}");
        Debug.Log($"Client Connected: {ClientOnConnectedCalled}");
        Debug.Log($"Client Disconnected: {ClientOnDisconnectedCalled}");
        Debug.Log($"Client Received Data: {ClientOnReceivedDataCalled}");
        yield return new WaitForConnectionOrTimeout(_client, _server, 30f);
        _server.Disconnect(1);
        yield return new WaitForDisconnectOrTimeout(_client, _server, 30f);
        _server.Stop();
        Debug.Log($"Server Connected: {ServerOnConnectedCalled}");
        Debug.Log($"Server Disconnected: {ServerOnDisconnectedCalled}");
        Debug.Log($"Server Received Data: {ServerOnReceivedDataCalled}");
        Debug.Log($"Client Connected: {ClientOnConnectedCalled}");
        Debug.Log($"Client Disconnected: {ClientOnDisconnectedCalled}");
        Debug.Log($"Client Received Data: {ClientOnReceivedDataCalled}");
        Assert.IsTrue(ServerOnConnectedCalled);
    }
}
