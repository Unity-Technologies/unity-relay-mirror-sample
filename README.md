# Info

Tutorial reference for this Unity + Mirror networking sample: https://www.youtube.com/c/ShrineApp

This sample was created to show an example implementation of Unity's Matchmaker, Multiplay, Vivox, 
Relay, Mirror, and UTP services in a simple Unity "game".

#  Example code - Multiplay Services

The main files that pertain to Vivox, Multiplay, and Matchmaker would be located within `Assests/Scripts`. Noteable files are:

- `VivoxManager.cs`
- `UnityRpc.cs`

VivoxManager deals with making calls to the Vivox SDK for various functionality from login, joining channels, and switching audio devices/volume.

UnityRpc is an Rpc that makes calls to our live backend hosted on AWS. This backend makes calls to the Multiplay, Matchmaker and Vivox APIs for various purposes such as allocating servers,
listing servers, requesting matches from matchmaker, polling matches, and requesting tokens. 

# Testing Vivox/Multiplay/matchmaker
## NOTE: This sample will not work properly unless the Project is associated with a Unity Project in the `Project Settings`. Due to our backend using our sample game credentials, this 
sample can't be tested unless the associated credentials match the backend therefore one must be added to the Wolfjaw Unity Project.

Open the project in Unity Edior.

Press play and then select Auth Login from the UI. -- This step logs into the backend service

Select Vivox login.

Now the user is presented with Multiple options for testing:
	- `Start matchmaking`
		This will make a call to the backend which in turns makes calls to the matchmaker API to create a match ticket and then polls for a match based on said ticket.
	- `Request Multiplay Server`
		This will send an allocation request to the backend which in turn makes a call to the Multiplay API and will allocate a server for the user.
	- `Server + Client` -- NOTE: There are multiple options for this, both will work. 
		This will create a server and connect a client. When the client connects, a Vivox Login and Join channel request will be sent to our backend. The backend will 
		retrieve Login and Join tokens from the Vivox API. These tokens are sent back to the game client and then are used to login to Vivox using the Vivox SDK within the sample. 

# Example Code - Networking Services

The specific files that pertain to Relay/Mirror/UTP are located in `Assets/UTPTransport` and `Assets/Mirror` respectively. Some files to note are:

- `UTPTransport/RelayNetworkingManager.cs`
- `UTPTransport/UtpTransport.cs`
- `Mirror/Runtime/NetworkManager.cs`
# Shrine Networking Tutorial Project
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

Code for the Unity + Mirror networking YouTube tutorials: https://www.youtube.com/c/ShrineApp
We are now at the point where the UTP transport and Relay functionality can be tested.

We have two files that deal with the implementations of these services, those files are:

- `Assets/scripts/MyNetworkManager`
	This is our custom NetworkManager which extends the RelayNetworkManager, the RelayNetworkManager in turn is extending Mirrors NetworkManager.
	Therefore our custom NetworkManager is able to leverage and showcase the abilities of both the services through a single NetowrkManager.
- `Assets/scripts/MenuUI`
	The UI element to our application which makes calls to our custom NetworkManager to start up clients/servers as needed with UI interactions.

# Testing Networking Services

Open the project in Unity Editor. 

We use a package called ParallelSync which allows us to launch two instances of the same project and test them in parallel. This should already be included in the sample.

Open the drop down from the "ParallelSync" tab in the toolbar at the top of the editor. 

Select "Clones Manager"

Select "Add new clone"

Once the new clone is created, a new box should appear in the "Clone Manager" as "Clone 0", select "Open in New Editor" for "Clone 0"

Now you should have two editors open with the same MirrorSample project.

Press play on both and select "Auth Login" on both. This will initiate a Unity login for Relay.

Now Mirror/Relay functionality can be tested. Test creating a server and joining between the two editors as needed. Make sure to use matching host/client buttons.

Standard Host/Client (DGS) = Mirror implementation

Relay Host/Client (Relay) = Relay Implementation

# Further Reading

More in-depth explanation on implementing these Multiplay services can be found within the README for the UTP assets lcoated in "Assests/UTPTransport"
For the source of Shrine Wars (a sample multiplayer game built with Mirror), see: https://github.com/ShrineGames/ShrineWars
The `Standard Host` button and the `Client (DGS)` button are used to launch a server using the UTP transport and connect to a server using the UTP transport respectively.

The `Relay Host` button and the `Client (Relay)` button are used to launch a Relay server and connect to a Relay server respectively.

Make sure that you are using matching buttons when testing.

# Further Reading
Additional information on the sample can be found in `Assets/UTPTransport/README.md`.
