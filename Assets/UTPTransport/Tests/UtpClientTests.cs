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
            _server = new UtpServer(timeoutInMilliseconds: 1000);

            _client = new UtpClient(
                () => { _clientOnConnectedCalled = true; },
                (message) => { _clientOnReceivedDataCalled = true; },
                () => { _clientOnDisconnectedCalled = true; },
                timeoutInMilliseconds: 1000
            );
        }

        [TearDown]
        public void TearDown()
        {
            _client.Disconnect();
            _server.Stop();

            _clientOnConnectedCalled = false;
            _clientOnDisconnectedCalled = false;
            _clientOnReceivedDataCalled = false;
        }

        [Test]
        public void IsConnected_NotConnected_False()
        {
            Assert.IsFalse(_client.IsConnected(), "Client is connected without calling Connect().");
        }

        [Test]
        public void IsConnected_NoServer_False()
        {
            _client.Connect("localhost", 7777);
            Assert.IsFalse(_client.IsConnected(), "Client is connected without server starting.");
        }

        [UnityTest]
        public IEnumerator IsConnected_WithServer_True()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);

            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            Assert.IsTrue(_client.IsConnected(), "Client was not able to connect with server.");
        }

        [UnityTest]
        public IEnumerator OnConnectedCallback_Called()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(_clientOnConnectedCalled, "The Client.OnConnected callback was not invoked as expected.");
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
            Assert.IsTrue(_clientOnDisconnectedCalled, "The Server.OnDisconnected callback was not invoked as expected.");
        }

        [UnityTest]
        public IEnumerator OnReceivedDataCallbacks_Called()
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
            Assert.IsTrue(_clientOnReceivedDataCalled, "The Client.OnReceivedData callback was not invoked as expected.");
        }
    }
}
