using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Utp;
using System.Threading.Tasks;

public class UtpTransportTests
{


    UtpTransport _Server;
    UtpTransport _Client;
    [SetUp]
    public void SetUp() {
        var ServerObj = new GameObject();
        var ClientObj = new GameObject();
        _Server = ServerObj.AddComponent<UtpTransport>();
        _Client = ClientObj.AddComponent<UtpTransport>();
    }
    [TearDown]
    public void TearDown() {
        GameObject.Destroy(_Server.gameObject);
        GameObject.Destroy(_Client.gameObject);
    }
    [Test]
    public void ServerActive_IsNotActive_False()
    {
        Assert.IsFalse(_Server.ServerActive(), "Server is running, but should not be.");
    }
    [Test]
    public void ServerActive_IsActive_True() {
        _Server.ServerStart();
        Assert.IsTrue(_Server.ServerActive(), "Server is not running, but should be.");
    }
    [Test]
    public void ServerStop_IsActive_False() {
        _Server.ServerStart();
        _Server.ServerStop();
        Assert.IsFalse(_Server.ServerActive());
    }
    [Test]
    public void ServerGetClientAddress_InvalidAddress_EmptyString() {
        string clientAddress = _Server.ServerGetClientAddress(0);
        Assert.IsEmpty(clientAddress);
    }
    [Test]
    public void ClientConnected_NotConnected_False() {
        Assert.IsFalse(_Client.ClientConnected(), "Client is connected, but should not be.");
    }
    [Test]
    public void ClientConnected_IsConnected_True() {
        _Server.ServerStart();
        _Client.ClientConnect(_Server.ServerUri());
        Assert.IsTrue(_Client.ClientConnected(), "Client is not connected, but should be.");
    }
}
