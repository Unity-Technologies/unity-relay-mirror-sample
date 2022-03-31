using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Utp;
using System.Threading.Tasks;
using Mirror;

public class UtpServerClientTests
{


    UtpServer _Server;
    UtpClient _Client;
    Transport _Transport;
    int TimeoutMS = 10000;
    IEnumerator DoTick(UtpClient client, UtpServer server) {
        int frameCounter = 5;
        while (frameCounter > 0)
        {
            client.Tick();
            server.Tick();
        frameCounter -= 1;
        yield return null;
        }
        yield return new WaitForSeconds(1f);
    }
    [SetUp]
    public void SetUp() {
        var Obj = new GameObject();
        _Server = new UtpServer(
			(connectionId) => _Transport.OnServerConnected.Invoke(connectionId),
			(connectionId, message) => _Transport.OnServerDataReceived.Invoke(connectionId, message, Channels.Reliable),
			(connectionId) => _Transport.OnServerDisconnected.Invoke(connectionId),
			TimeoutMS
        );
        _Client = new UtpClient(
			() => _Transport.OnClientConnected.Invoke(),
			(message) => _Transport.OnClientDataReceived.Invoke(message, Channels.Reliable),
			() => _Transport.OnClientDisconnected.Invoke(),
			TimeoutMS
        );
    }
    [Test]
    public void Server_IsActive_NotStarted_False() {
        Assert.IsFalse(_Server.IsActive());
    }
    [Test]
    public void Server_IsActive_Started_True() {
        _Server.Start(7777);
        Assert.IsTrue(_Server.IsActive());
    }
    [UnityTest]
    public IEnumerator Client_IsConnected_NotConnected_False() {
        Assert.IsFalse(_Client.IsConnected());
        yield return null;
    }
    [UnityTest]
    public IEnumerator Client_IsConnected_NoServer_False() {
        _Client.Connect("localhost", 7777);
        Assert.IsFalse(_Client.IsConnected());
        yield return null;
    }
    [UnityTest]
    public IEnumerator Client_IsConnected_WithServer_True() {
        _Server.Start(7777);
        _Client.Connect("localhost", 7777);
        yield return DoTick(_Client, _Server);
        Assert.IsTrue(_Client.IsConnected());
    }
    [Test]
    public void Server_GetClientAddress_NotConnected_EmptyString() {
        string clientAddress = _Server.GetClientAddress(0);
        Assert.IsEmpty(clientAddress);
    }
    [UnityTest]
    public IEnumerator Server_GetClientAddress_Connected_NonEmptyString() {
        _Server.Start(7777);
        _Client.Connect("localhost", 7777);
        yield return DoTick(_Client, _Server);
        Assert.IsTrue(_Client.IsConnected());
        string clientAddress = _Server.GetClientAddress(1);
        Assert.IsNotEmpty(clientAddress);
    }
}
