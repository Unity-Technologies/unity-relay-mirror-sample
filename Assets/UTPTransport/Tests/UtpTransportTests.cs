using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Utp
{
    public class UtpTransportTests
    {
        private UtpTransport _server;
        private UtpTransport _client;

        [SetUp]
        public void SetUp()
        {
            var serverObj = new GameObject();
            _server = serverObj.AddComponent<UtpTransport>();

            var clientObj = new GameObject();
            _client = clientObj.AddComponent<UtpTransport>();
        }

        [TearDown]
        public void TearDown()
        {
            _client.ClientDisconnect();
            GameObject.Destroy(_client.gameObject);

            _server.ServerStop();
            GameObject.Destroy(_server.gameObject);
        }

        [Test]
        public void ServerActive_IsNotActive_False()
        {
            Assert.IsFalse(_server.ServerActive(), "Server is running, but should not be.");
        }

        [Test]
        public void ServerActive_IsActive_True()
        {
            _server.ServerStart();
            Assert.IsTrue(_server.ServerActive(), "Server is not running, but should be.");
        }

        [Test]
        public void ServerStop_IsActive_False()
        {
            _server.ServerStart();
            _server.ServerStop();
            Assert.IsFalse(_server.ServerActive(), "Server is running, but should not be.");
        }

        [Test]
        public void ServerGetClientAddress_InvalidAddress_EmptyString()
        {
            string clientAddress = _server.ServerGetClientAddress(0);
            Assert.IsEmpty(clientAddress, "A client address was returned instead of an empty string.");
        }

        [UnityTest]
        public IEnumerator ServerGetClientAddress_clientConnected_NonEmptyString()
        {
            _server.ServerStart();
            _client.ClientConnect(_server.ServerUri());
            yield return new WaitForTransportToConnect(_client, _server, 30f);
            int idOfFirstClient = 1;
            string clientAddress = _server.ServerGetClientAddress(idOfFirstClient);
            Assert.IsNotEmpty(clientAddress, "A client address was not returned, connection possibly timed out..");
        }

        [Test]
        public void ClientConnected_NotConnected_False()
        {
            Assert.IsFalse(_client.ClientConnected(), "Client is connected, but should not be.");
        }

        [UnityTest]
        public IEnumerator ClientConnected_IsConnected_True()
        {
            _server.ServerStart();
            _client.ClientConnect(_server.ServerUri());
            yield return new WaitForTransportToConnect(_client, _server, 30f);
            Assert.IsTrue(_client.ClientConnected(), "Client is not connected, but should be.");
        }
    }
}
