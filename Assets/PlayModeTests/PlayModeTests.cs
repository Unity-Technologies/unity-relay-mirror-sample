using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;

public class PlayModeTests
{
    [Test]
    public void UtpTransportTests()
    {
        var Obj = new GameObject();
        Utp.UtpTransport _Transport = Obj.AddComponent<Utp.UtpTransport>();
        Assert.IsTrue(_Transport.Available());
        Assert.IsFalse(_Transport.ServerActive());
        _Transport.ServerStart();
        Assert.IsTrue(_Transport.ServerActive());
        Assert.IsNotNull(_Transport.ServerUri());
        _Transport.ServerStop();
        Assert.IsFalse(_Transport.ServerActive());
    }
}
