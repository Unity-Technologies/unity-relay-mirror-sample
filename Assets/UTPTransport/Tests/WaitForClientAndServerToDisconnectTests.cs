using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;

namespace Utp
{
    public class WaitForClientAndServerToDisconnectTests
    {
        private UtpServer _server;
        private UtpClient _client;

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

        [UnityTest]
        public IEnumerator WaitForClientAndServerToDisconnect_NoConnections_ClientDisconnected()
        {
            WaitForClientAndServerToDisconnect connectionTestResult = new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 5f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForClientAndServerToDisconnect.Status.ClientDisconnected);
        }

        [UnityTest]
        public IEnumerator WaitForClientAndServerToDisconnect_ClientConnected_NoDisconnect_TimeoutReached()
        {
            _server.Start(port: 7777);
            _client.Connect(host: "localhost", port: 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            WaitForClientAndServerToDisconnect connectionTestResult = new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 5f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForClientAndServerToDisconnect.Status.TimedOut);
        }

        [UnityTest]
        public IEnumerator WaitForClientAndServerToDisconnect_ClientConnected_ServerDisconnect_ClientDisconnected()
        {
            _server.Start(port: 7777);
            _client.Connect(host: "localhost", port: 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            int idOfFirstClient = 1;
            _server.Disconnect(connectionId: idOfFirstClient);
            WaitForClientAndServerToDisconnect connectionTestResult = new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 30f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForClientAndServerToDisconnect.Status.ClientDisconnected);
        }
    }
}
