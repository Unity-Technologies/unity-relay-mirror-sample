# Info

This sample was created to demonstrate how to utilize the Unity Transport Package (UTP) and Relay service with the Mirror networking API.

[Shrine's Mirror Networking series on YouTube](https://www.youtube.com/c/ShrineApp) was used as reference for this sample.

#  Example code

The files for UTP and Mirror are located in `Assets/UTPTransport` and `Assets/Mirror` respectively. 

Mirror:
- `Assets/Mirror/Runtime/NetworkManager.cs` - Responsible for managing the networking aspects of a multiplayer game. 

UTP:
- `Assets/UTPTransport/RelayNetworkManager.cs` - Extends `Mirror.NetworkManager` with Relay capabilities.
- `Assets/UTPTransport/UtpTransport.cs` - A `Mirror.Transport` compatible with UTP.

Sample Code:
- `Assets/Scripts/MyNetworkManager.cs` - This class is meant to demonstrate the functionality of the UTP transport for Mirror and the Relay service.
- `Assets/Scripts/MenuUI.cs`- This class is responsible for displaying the UI that drives the sample, it interfaces with `MyNetworkManager` to launch servers and connect clients.

# Testing
Use the following steps to launch two instances of the sample project for testing purposes:
1. Open the project in Unity Editor. 
2. Select `ParallelSync > Clones Manager` from the editor dropdown, we use ParallelSync to launch multiple editor instances of the same project.
3. Press the `Add new clone` button.
4. Press the `Open in New Editor` button once the clone has been created.
5. Press the Play button in both editor instances.
6. Press the `Auth Login` button on both editor instances, this will authenticate with the Unity Authentication Service for Relay.

We are now at the point where the UTP transport and Relay functionality can be tested.

The `Standard Host` button and the `Client (DGS)` button are used to launch a server using the UTP transport and connect to a server using the UTP transport respectively.

The `Relay Host` button and the `Client (Relay)` button are used to launch a Relay server and connect to a Relay server respectively.

Make sure that you are using matching buttons when testing.

# Further Reading
Additional information on the sample can be found in `Assets/UTPTransport/README.md`.