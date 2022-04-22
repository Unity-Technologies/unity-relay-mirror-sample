using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;

namespace Utp
{
    public class WaitForClientAndServerToConnectTests
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
        public IEnumerator NoConnections_StatusTimedOut()
        {
            WaitForClientAndServerToConnect connectionTestResult = new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 5f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForClientAndServerToConnect.Status.TimedOut);
        }

        [UnityTest]
        public IEnumerator ClientConnected_StatusClientConnected()
        {
            _server.Start(port: 7777);
            _client.Connect(host: "localhost", port: 7777);
            WaitForClientAndServerToConnect connectionTestResult = new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            yield return connectionTestResult;
            Assert.IsTrue(connectionTestResult.Result == WaitForClientAndServerToConnect.Status.ClientConnected);
        }
    }
}
