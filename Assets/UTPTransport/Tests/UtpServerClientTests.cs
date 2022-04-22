using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Utp
{
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
        public IEnumerator TickFrames(UtpClient _Client, UtpServer _Server, int FramesToSkip = 15)
        {
            int FramesPassed = 0;
            while (FramesPassed < FramesToSkip)
            {
                _Client.Tick();
                _Server.Tick();
                yield return null;
                FramesPassed++;
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
            ServerOnConnectedCalled = false;
            ServerOnDisconnectedCalled = false;
            ServerOnReceivedDataCalled = false;
            ClientOnConnectedCalled = false;
            ClientOnDisconnectedCalled = false;
            ClientOnReceivedDataCalled = false;
        }

        [UnityTest]
        public IEnumerator WaitForConnectionOrTimeout_NoConnections_StatusTimedOut()
        {
            WaitForConnectionOrTimeout connectionTestResult = new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 5f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForConnectionOrTimeout.Status.TimedOut);
        }

        [UnityTest]
        public IEnumerator WaitForConnectionOrTimeout_ClientConnected_StatusClientConnected()
        {
            _server.Start(port: 7777);
            _client.Connect(host: "localhost", port: 7777);
            WaitForConnectionOrTimeout connectionTestResult = new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForConnectionOrTimeout.Status.ClientConnected);
        }

        [UnityTest]
        public IEnumerator WaitForDisconnectOrTimeout_NoConnections_ClientDisconnected()
        {
            WaitForDisconnectOrTimeout connectionTestResult = new WaitForDisconnectOrTimeout(client: _client, server: _server, timeoutInSeconds: 5f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForDisconnectOrTimeout.Status.ClientDisconnected);
        }

        [UnityTest]
        public IEnumerator WaitForDisconnectOrTimeout_ClientConnected_NoDisconnect_TimeoutReached()
        {
            _server.Start(port: 7777);
            _client.Connect(host: "localhost", port: 7777);
            yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            WaitForDisconnectOrTimeout connectionTestResult = new WaitForDisconnectOrTimeout(client: _client, server: _server, timeoutInSeconds: 5f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForDisconnectOrTimeout.Status.TimedOut);
        }

        [UnityTest]
        public IEnumerator WaitForDisconnectOrTimeout_ClientConnected_ServerDisconnect_ClientDisconnected()
        {
            _server.Start(port: 7777);
            _client.Connect(host: "localhost", port: 7777);
            yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            int idOfFirstClient = 1;
            _server.Disconnect(connectionId: idOfFirstClient);
            WaitForDisconnectOrTimeout connectionTestResult = new WaitForDisconnectOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForDisconnectOrTimeout.Status.ClientDisconnected);
        }

        [Test]
        public void UtpServer_IsActive_NotStarted_False()
        {
            Assert.IsFalse(_server.IsActive(), "Server is active without being started.");
        }

        [Test]
        public void UtpServer_IsActive_Started_True()
        {
            _server.Start(7777);
            Assert.IsTrue(_server.IsActive(), "Server did not start and is not active.");
        }

        [Test]
        public void UtpClient_IsConnected_NotConnected_False()
        {
            Assert.IsFalse(_client.IsConnected(), "Client is connected without calling Connect().");
        }

        [Test]
        public void UtpClient_IsConnected_NoServer_False()
        {
            _client.Connect("localhost", 7777);
            Assert.IsFalse(_client.IsConnected(), "Client is connected without server starting.");
        }

        [UnityTest]
        public IEnumerator UtpClient_IsConnected_WithServer_True()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);

            yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);

            Assert.IsTrue(_client.IsConnected(), "Client was not able to connect with server.");
        }

        [Test]
        public void UtpServer_GetClientAddress_NotConnected_EmptyString()
        {
            int idOfNonExistentClient = 1;
            string clientAddress = _server.GetClientAddress(idOfNonExistentClient);
            Assert.IsEmpty(clientAddress, "Client address was not empty.");
        }

        [UnityTest]
        public IEnumerator UtpServer_GetClientAddress_Connected_NonEmptyString()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);

            yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);

            int idOfFirstClient = 1;
            string clientAddress = _server.GetClientAddress(idOfFirstClient);
            Assert.IsNotEmpty(clientAddress, "Client address was empty, indicating the client is probably not connected.");
        }

        [Test]
        public void UtpServer_Disconnect_NoClient_Warning()
        {
            int idOfNonExistentClient = 1;
            _server.Disconnect(idOfNonExistentClient);
            LogAssert.Expect(LogType.Warning, "Connection not found: 1");

        }
        [UnityTest]
        public IEnumerator UtpServer_Disconnect_ClientConnected_Success()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(_client.IsConnected(), "Client did not connect to server.");
            int idOfFirstClient = 1;
            _server.Disconnect(idOfFirstClient);
            yield return new WaitForDisconnectOrTimeout(client: _client, server: _server, timeoutInSeconds: 10f);
            Assert.IsFalse(_client.IsConnected(), "Client was not successfully disconnected from server");
        }
        [Test]
        public void UtpServer_FindConnection_NoClient_DefaultConnection()
        {
            int idOfNonExistentClient = 1;
            Unity.Networking.Transport.NetworkConnection FoundConnection = _server.FindConnection(idOfNonExistentClient);
            Assert.IsTrue(FoundConnection == default(Unity.Networking.Transport.NetworkConnection), "A connection was found when no client was connected.");
        }
        [UnityTest]
        public IEnumerator UtpServer_FindConnection_ClientConnected_ValidConnection()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            int idOfFirstClient = 1;
            yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            Unity.Networking.Transport.NetworkConnection FoundConnection = _server.FindConnection(idOfFirstClient);
            Assert.IsFalse(FoundConnection == default(Unity.Networking.Transport.NetworkConnection), "No connection found.");
        }

        [UnityTest]
        public IEnumerator UtpServer_OnConnectedCallback_Called()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(ServerOnConnectedCalled, "The Server.OnConnected callback was not invoked as expected.");
        }

        [UnityTest]
        public IEnumerator UtpClient_OnConnectedCallback_Called()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(ClientOnConnectedCalled, "The Client.OnConnected callback was not invoked as expected.");
        }

        [UnityTest]
        public IEnumerator UtpServer_OnDisconnectedCallback_Called()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            yield return TickFrames(_client, _server, 5);
            int idOfFirstClient = 1;
            _server.Disconnect(idOfFirstClient);
            yield return new WaitForDisconnectOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(ClientOnDisconnectedCalled, "The UtpClient.OnDisconnected callback was not invoked as expected.");
        }

        [UnityTest]
        public IEnumerator UtpClient_OnDisconnectedCallback_Called()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            yield return TickFrames(_client, _server, 5);
            int idOfFirstClient = 1;
            _server.Disconnect(idOfFirstClient);
            yield return new WaitForDisconnectOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(ServerOnDisconnectedCalled, "The Server.OnDisconnected callback was not invoked as expected.");
        }

        [UnityTest]
        public IEnumerator UtpServer_OnReceivedDataCallback_Called()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            int idOfFirstClient = 1;
            int idOfChannel = 1;
            ArraySegment<byte> emptyPacket = new ArraySegment<byte>(new byte[4]);
            _client.Send(emptyPacket, idOfChannel);
            _server.Send(idOfFirstClient, emptyPacket, idOfChannel);
            yield return TickFrames(_client, _server, 5);
            Assert.IsTrue(ServerOnReceivedDataCalled, "The Server.OnReceivedData callback was not invoked as expected.");
        }

        [UnityTest]
        public IEnumerator UtpClient_OnReceivedDataCallbacks_Called()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            int idOfFirstClient = 1;
            int idOfChannel = 1;
            ArraySegment<byte> emptyPacket = new ArraySegment<byte>(new byte[4]);
            _client.Send(emptyPacket, idOfChannel);
            _server.Send(idOfFirstClient, emptyPacket, idOfChannel);
            yield return TickFrames(_client, _server, 5);
            Assert.IsTrue(ClientOnReceivedDataCalled, "The Client.OnReceivedData callback was not invoked as expected.");
        }
    }
}
