using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine.TestTools;

namespace Utp
{
    public class UtpClientTests
    {
        private UtpServer _server;
        private UtpClient _client;
        private bool ServerOnConnectedCalled;
        private bool ServerOnDisconnectedCalled;
        private bool ServerOnReceivedDataCalled;
        private bool ClientOnConnectedCalled;
        private bool ClientOnDisconnectedCalled;
        private bool ClientOnReceivedDataCalled;

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

            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            Assert.IsTrue(_client.IsConnected(), "Client was not able to connect with server.");
        }

        [UnityTest]
        public IEnumerator UtpClient_OnConnectedCallback_Called()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(ClientOnConnectedCalled, "The Client.OnConnected callback was not invoked as expected.");
        }

        [UnityTest]
        public IEnumerator UtpClient_OnDisconnectedCallback_Called()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            yield return TickFrames(_client, _server, 5);
            int idOfFirstClient = 1;
            _server.Disconnect(idOfFirstClient);
            yield return new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(ServerOnDisconnectedCalled, "The Server.OnDisconnected callback was not invoked as expected.");
        }

        [UnityTest]
        public IEnumerator UtpClient_OnReceivedDataCallbacks_Called()
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
            Assert.IsTrue(ClientOnReceivedDataCalled, "The Client.OnReceivedData callback was not invoked as expected.");
        }
    }
}
