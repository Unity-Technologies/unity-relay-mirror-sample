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
    [Test]
    public void Server_GetClientAddress_NotConnected_EmptyString() {
        string clientAddress = _Server.GetClientAddress(0);
        Assert.IsEmpty(clientAddress);
    }
    [Test]
    public void Server_GetClientAddress_Connected_NonEmptyString() {
        _Server.Start(7777);
        _Client.Connect("localhost", 7777);
        _Server.ProcessIncomingEvents();
        string clientAddress = _Server.GetClientAddress(1);
        Assert.IsNotEmpty(clientAddress);
    }
    [Test]
    public void Client_IsConnected_NotConnected_False() {
        Assert.IsFalse(_Client.IsConnected());
    }
    [UnityTest]
    public IEnumerator Client_IsConnected_NoServer_False() {
        _Client.Connect("localhost", 7777);
        yield return new WaitForSeconds(11f);
        Assert.IsFalse(_Client.IsConnected());
    }
    [Test]
    public void Client_IsConnected_WithServer_True() {
        _Server.Start(7777);
        _Client.Connect("localhost", 7777);
        Assert.IsTrue(_Client.IsConnected());
    }
}
