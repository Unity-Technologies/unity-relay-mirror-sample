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
            _server = ServerObj.AddComponent<UtpTransport>();

            var ClientObj = new GameObject();
            _client = ClientObj.AddComponent<UtpTransport>();
        }
        [TearDown]
        public void TearDown()
        {
            _client.ClientDisconnect();
            GameObject.Destroy(_client.gameObject);

            _server.ServerStop();
            GameObject.Destroy(_server.gameObject);
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
            _server.ServerStart();
            _client.ClientConnect(_server.ServerUri());
            WaitForConnectionOrTimeout connectionTestResult = new WaitForConnectionOrTimeout(client: _client, server: _server, timeoutInSeconds: 30f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForConnectionOrTimeout.Status.ClientConnected);
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
            yield return new WaitForConnectionOrTimeout(_client, _server, 30f);
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
            yield return new WaitForConnectionOrTimeout(_client, _server, 30f);
            Assert.IsTrue(_client.ClientConnected(), "Client is not connected, but should be.");
        }
    }
}
