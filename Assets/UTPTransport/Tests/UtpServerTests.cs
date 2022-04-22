using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Utp
{
    public class UtpServerTests
    {
        private UtpServer _server;
        private UtpClient _client;
        private bool _serverOnConnectedCalled;
        private bool _serverOnDisconnectedCalled;
        private bool _serverOnReceivedDataCalled;
        private bool _clientOnConnectedCalled;
        private bool _clientOnDisconnectedCalled;
        private bool _clientOnReceivedDataCalled;

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
                (connectionId) => { _serverOnConnectedCalled = true; },
                (connectionId, message) => { _serverOnReceivedDataCalled = true; },
                (connectionId) => { _serverOnDisconnectedCalled = true; },
                timeout: 1000
            );

            _client = new UtpClient(
                () => { _clientOnConnectedCalled = true; },
                (message) => { _clientOnReceivedDataCalled = true; },
                () => { _clientOnDisconnectedCalled = true; },
                timeout: 1000
            );
        }

        [TearDown]
        public void TearDown()
        {
            _client.Disconnect();
            _server.Stop();
            _serverOnConnectedCalled = false;
            _serverOnDisconnectedCalled = false;
            _serverOnReceivedDataCalled = false;
            _clientOnConnectedCalled = false;
            _clientOnDisconnectedCalled = false;
            _clientOnReceivedDataCalled = false;
        }

        [Test]
        public void IsActive_NotStarted_False()
        {
            Assert.IsFalse(_server.IsActive(), "Server is active without being started.");
        }

        [Test]
        public void IsActive_Started_True()
        {
            _server.Start(7777);
            Assert.IsTrue(_server.IsActive(), "Server did not start and is not active.");
        }

        [Test]
        public void GetClientAddress_NotConnected_EmptyString()
        {
            int idOfNonExistentClient = 1;
            string clientAddress = _server.GetClientAddress(idOfNonExistentClient);
            Assert.IsEmpty(clientAddress, "Client address was not empty.");
        }

        [UnityTest]
        public IEnumerator GetClientAddress_Connected_NonEmptyString()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);

            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            int idOfFirstClient = 1;
            string clientAddress = _server.GetClientAddress(idOfFirstClient);
            Assert.IsNotEmpty(clientAddress, "Client address was empty, indicating the client is probably not connected.");
        }

        [Test]
        public void Disconnect_NoClient_Warning()
        {
            int idOfNonExistentClient = 1;
            _server.Disconnect(idOfNonExistentClient);
            LogAssert.Expect(LogType.Warning, "Connection not found: 1");

        }

        [UnityTest]
        public IEnumerator Disconnect_ClientConnected_Success()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(_client.IsConnected(), "Client did not connect to server.");
            int idOfFirstClient = 1;
            _server.Disconnect(idOfFirstClient);
            yield return new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 10f);
            Assert.IsFalse(_client.IsConnected(), "Client was not successfully disconnected from server");
        }

        [Test]
        public void FindConnection_NoClient_DefaultConnection()
        {
            int idOfNonExistentClient = 1;
            Unity.Networking.Transport.NetworkConnection FoundConnection = _server.FindConnection(idOfNonExistentClient);
            Assert.IsTrue(FoundConnection == default(Unity.Networking.Transport.NetworkConnection), "A connection was found when no client was connected.");
        }

        [UnityTest]
        public IEnumerator FindConnection_ClientConnected_ValidConnection()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            int idOfFirstClient = 1;
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            Unity.Networking.Transport.NetworkConnection FoundConnection = _server.FindConnection(idOfFirstClient);
            Assert.IsFalse(FoundConnection == default(Unity.Networking.Transport.NetworkConnection), "No connection found.");
        }

        [UnityTest]
        public IEnumerator OnConnectedCallback_Called()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(_serverOnConnectedCalled, "The Server.OnConnected callback was not invoked as expected.");
        }

        [UnityTest]
        public IEnumerator OnDisconnectedCallback_Called()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            yield return TickFrames(_client, _server, 5);
            int idOfFirstClient = 1;
            _server.Disconnect(idOfFirstClient);
            yield return new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(_clientOnDisconnectedCalled, "The UtpClient.OnDisconnected callback was not invoked as expected.");
        }

        [UnityTest]
        public IEnumerator OnReceivedDataCallback_Called()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            int idOfFirstClient = 1;
            int idOfChannel = 1;
            ArraySegment<byte> emptyPacket = new ArraySegment<byte>(new byte[4]);
            _client.Send(emptyPacket, idOfChannel);
            _server.Send(idOfFirstClient, emptyPacket, idOfChannel);
            yield return TickFrames(_client, _server, 5);
            Assert.IsTrue(_serverOnReceivedDataCalled, "The Server.OnReceivedData callback was not invoked as expected.");
        }
    }
}
