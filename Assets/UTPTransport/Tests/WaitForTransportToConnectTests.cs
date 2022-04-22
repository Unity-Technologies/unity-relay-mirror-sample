using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Utp
{
    public class WaitForTransportToConnectTests
    {
        private UtpTransport _server;
        private UtpTransport _client;

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
        public IEnumerator NoConnections_StatusTimedOut()
        {
            WaitForTransportToConnect connectionTestResult = new WaitForTransportToConnect(client: _client, server: _server, timeoutInSeconds: 5f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForTransportToConnect.Status.TimedOut);
        }

        [UnityTest]
        public IEnumerator ClientConnected_StatusClientConnected()
        {
            _server.ServerStart();
            _client.ClientConnect(_server.ServerUri());
            WaitForTransportToConnect connectionTestResult = new WaitForTransportToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForTransportToConnect.Status.ClientConnected);
        }
    }
}
