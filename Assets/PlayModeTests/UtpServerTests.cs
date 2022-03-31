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

public class UtpServerTests
{


    UtpServer _Server;
    UtpClient _Client;
    Transport _Transport;
    [SetUp]
    public void SetUp() {
        var Obj = new GameObject();
        _Transport = Obj.AddComponent<Transport>();
        _Server = new UtpServer(
			(connectionId) => _Transport.OnServerConnected.Invoke(connectionId),
			(connectionId, message) => _Transport.OnServerDataReceived.Invoke(connectionId, message, Channels.Reliable),
			(connectionId) => _Transport.OnServerDisconnected.Invoke(connectionId),
			1000
        );
        _Client = new UtpClient(
			() => _Transport.OnClientConnected.Invoke(),
			(message) => _Transport.OnClientDataReceived.Invoke(message, Channels.Reliable),
			() => _Transport.OnClientDisconnected.Invoke(),
			1000
        );
    }
    [Test]
    public void IsActive_NotStarted_False() {
        Assert.IsFalse(_Server.IsActive());
    }
    [Test]
    public void IsActive_Started_True() {
        _Server.Start(7777);
        Assert.IsTrue(_Server.IsActive());
    }
    [Test]
    public void GetClientAddress_NotConnected_EmptyString() {
        string clientAddress = _Server.GetClientAddress(0);
        Assert.IsEmpty(clientAddress);
    }
    [UnityTest]
    public IEnumerator GetClientAddress_Connected_NonEmptyString() {
        _Server.Start(7777);
        yield return null;
        _Client.Connect("localhost", 7777);
        yield return new WaitForSeconds(5f);
        string clientAddress = _Server.GetClientAddress(0);
        Assert.IsNotEmpty(clientAddress);
    }
}
