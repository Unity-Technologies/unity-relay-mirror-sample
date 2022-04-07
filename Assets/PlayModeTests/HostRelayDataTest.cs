using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Utp;

public class HostRelayDataTest
{
    [UnityTest]
    public IEnumerator HostRelayData_ReturnValueIsNotNull_True()
    {
        //Create & instantiate new gameobject
        GameObject gameObject = new GameObject();
        gameObject = GameObject.Instantiate(gameObject);

        //Add relay manager component to gameobject
        RelayManager relayManager = gameObject.AddComponent<RelayManager>();

        //Allocate a relay server
        relayManager.AllocateRelayServer(1, "us-east4");

        //Wait 5 seconds to ensure allocation has been completed
        yield return new WaitForSeconds(5.0f);

        //Get and assert server data is not null
        RelayServerData serverData = RelayUtils.HostRelayData(relayManager.serverAllocation, "udp");
        Assert.IsNotNull(serverData);
    }
}
