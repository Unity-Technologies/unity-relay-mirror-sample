using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Utp
{
    public class UtpClientTests
    {
        private UtpServer _server;
        private UtpClient _client;

        [SetUp]
        public void SetUp()
        {
            _server = new UtpServer(timeoutInMilliseconds: 1000);
            _client = new UtpClient(timeoutInMilliseconds: 1000);
        }

        [TearDown]
        public void TearDown()
        {
            _client.Disconnect();
            _server.Stop();
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
            bool callbackWasInvoked = false;
            _client.OnConnected += () => { callbackWasInvoked = true; };
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(callbackWasInvoked, "The Client.OnConnected callback was not invoked as expected.");
        }

        [UnityTest]
        public IEnumerator OnDisconnectedCallback_Called()
        {
            _server.Start(7777);
            bool callbackWasInvoked = false;
            _client.OnDisconnected += () => { callbackWasInvoked = true; };
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            yield return tickClientAndServerForSeconds(_client, _server, 5f);
            int idOfFirstClient = 1;
            _server.Disconnect(idOfFirstClient);
            yield return new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 30f);
            Assert.IsTrue(callbackWasInvoked, "The Server.OnDisconnected callback was not invoked as expected.");
        }

        [UnityTest]
        public IEnumerator OnReceivedDataCallbacks_Called()
        {
            _server.Start(7777);
            bool callbackWasInvoked = false;
            _client.OnReceivedData += (segment) => { callbackWasInvoked = true; };
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            int idOfFirstClient = 1;
            int idOfChannel = 1;
            ArraySegment<byte> emptyPacket = new ArraySegment<byte>(new byte[4]);
            _client.Send(emptyPacket, idOfChannel);
            _server.Send(idOfFirstClient, emptyPacket, idOfChannel);
            yield return tickClientAndServerForSeconds(_client, _server, 5f);
            Assert.IsTrue(callbackWasInvoked, "The Client.OnReceivedData callback was not invoked as expected.");
        }

        private IEnumerator tickClientAndServerForSeconds(UtpClient client, UtpServer server, float numSeconds)
        {
            float elapsedTime = 0f;
            while (elapsedTime < numSeconds)
            {
                client.Tick();
                server.Tick();
                yield return null;
                elapsedTime += Time.deltaTime;
            }
        }
    }
}
