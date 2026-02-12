# Unity Relay Mirror Sample

The Unity Relay Mirror Sample demonstrates how to use the [Unity Transport Package](https://docs.unity3d.com/Packages/com.unity.transport@latest), the [Unity Relay service](https://docs.unity.com/relay), and the [Mirror Networking library](https://mirror-networking.com/) together. 

* The Unity Transport Package is a low-level networking library that provides a connection-based abstraction layer over UDP sockets with optional functionality such as reliability, ordering, and fragmentation.
* Relay is a Unity service that facilitates securely connecting players by using a join code style workflow without the need for dedicated game servers or peer-to-peer communication.
* The Mirror Networking library is a high-level networking library for the Unity Platform.

The [Unity Relay](https://docs.unity.com/relay) documentation contains additional information on the usage of this sample.
## Requirements
The sample has the following requirements:
* [Unity Editor version 6000.2.10f1](https://unity.com/releases/editor/whats-new/6000.2.10f1)
* Unity services
	* [Unity Authentication Service](https://docs.unity.com/authentication)
	* [Unity Relay Service](https://docs.unity.com/relay)
* Unity packages
	* [Unity Multiplayer Services](https://docs.unity3d.com/Packages/com.unity.services.multiplayer@1.1/manual/index.html) 
	* [Unity Transport](https://docs.unity3d.com/Packages/com.unity.transport@latest) 
    * [Unity Jobs](https://docs.unity3d.com/Packages/com.unity.jobs@latest)
* [Mirror Networking library](https://mirror-networking.com/)
## Installation
If you would like to use the code from this sample in your own project, please perform the following steps:
1. Install the latest version of Mirror.
    * The latest version of Mirror can be obtained from either the [Unity Asset Store](https://assetstore.unity.com/packages/tools/network/mirror-129321) or the [Mirror repository on Github](https://github.com/vis2k/mirror).
2. Install the latest version of the `com.unity.jobs` package using the Unity Package Manager.
3. Install the latest version of the `com.unity.services.relay` package using the Unity Package Manager.
4. Copy the `Assets/UTPTransport` folder from this sample into the `Assets/` directory of your own project.

## Community and Feedback
The Unity Relay Mirror Sample is an open-source project and we encourage and welcome
contributions. If you wish to contribute, be sure to review our
[contribution guidelines](CONTRIBUTING.md).
