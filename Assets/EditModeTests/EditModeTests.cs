using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EditModeTests
{
    Utp.RelayManager _RelayManager;
    // A Test behaves as an ordinary method
    [Test]
    public void EditModeTestsSimplePasses()
    {
        // Use the Assert class to test conditions
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator EditModeTestsWithEnumeratorPasses()
    {
        _RelayManager.AllocateRelayServer(5, "us-west-1");  // Doing this causes null reference error? Still have a lot of C# to learn.
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
