# Info

Tutorial reference for this Unity + Mirror networking sample: https://www.youtube.com/c/ShrineApp

This sample was created to show an example implementation of Unity's Relay, Mirror, and UTP services.

This sample game has been stripped of all services/code not relating to Unity's Relay, Mirror, and UTP as a showcase for these services.

#  Example code

The specific files that pertain to Relay/Mirror/UTP are located in `Assets/UTPTransport` and `Assets/Mirror` respectively. Some files to note are:

- `UTPTransport/RelayNetworkingManager.cs`
- `UTPTransport/UtpTransport.cs`
- `Mirror/Runtime/NetworkManager.cs`


We have two files that deal with the implementations of these services, those files are:

- `Assets/scripts/MyNetworkManager`
	This is our custom NetworkManager which extends the RelayNetworkManager, the RelayNetworkManager in turn is extending Mirrors NetworkManager.
	Therefore our custom NetworkManager is able to leverage and showcase the abilities of both the services through a single NetowrkManager.
- `Assets/scripts/MenuUI`
	The UI element to our application which makes calls to our custom NetworkManager to start up clients/servers as needed with UI interactions.

# Testing

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

More in-depth explanation on implementing these services can be found within the README for the UTP assets lcoated in "Assests/UTPTransport"