# Getting Up and Running

## High Level Information

This project has three components.

1. The Game Client (the game that runs on players machines)
1. The Game Server (the server binary that can be run locally for development, but is hosted via Multiplay in production)
1. The Game Backend (a backend service that can be run locally for development, but is hosted by your studio in production)

Both the game client and game server are built using unity and the Mirror sample.

The game backend in this example is a [Golang](https://golang.org/) application. There is not a requirement for you to use a Golang application for your backend service, it is just what we decided to use for demonstration. It provides a very basic user management system along with examples for how to generate Vivox tokens, manage game servers with Multiplay, and use the Unity Matchmaker.

## Game Client and Game Server

You will need Unity Editor version 2020.3.24f1, this can be obtained through the Unity Hub or on the Unity website itself. The MirrorSample was created using this version and requires it to be opened properly.

### Windows

1. Unity Hub and Unity Editor [Unity Hub + Editor](https://unity3d.com/get-unity/download) 

### Linux Cross-Compiling

1. To cross-compile for Linux, you will need to go into the Unity Hub and select the installs section on the left hand side.

2. Find your version number (2020.3.24f1) and select the gear icon next to it. Then select add modules.

3. Scroll down and check in both the module for Linux Build Support (IL2CPP & Mono) and press the install. You will now be able to select Linux as a build option within Unity.

### General Unity Notes

The first time opeing the project will take a couple minutes to import all the files/packages

### Mirror Sample Game

Open the project within Unity. To run simply place the play button at the top of the window to run in editor.

```
Mirror Sample
```

If you would like to run a standalone client just run the built exe file through command line or by selecting it.

If you'd like to launch a dedicated server, you may run with the following commandline args. 

```
-queryport 7778 -version 001 -queryprotocol sqp
```

The `queryport`, `version`, and `queryprotocol` commandline args are only necessary if you intend to issue server queries against the game server. More information can be found at `UnityMirrorTutorials-master/Docs/Server_Query/README.md`.

### Packaging

To package either the game client or game server.

1. Launch the Editor

2. Go to File -> Build Settings... 

3. Under "Target Platform" select your desired platform

4. if packaging a server, select "server build" otherwise leave the box unchecked and select "Build" and choose a build location

### Running Both a Client and Server

First, you will need to build whichever target you do not intend to attach the debugger to. Once you do that, you may find the binary for it in `UnityMirrorTutorials-master/Binaries/`. To view logs in real-time, you will need to append a `-log` argument when running the binary. It is particularly important to add this argument when running a server, as if you do not the process will be started in the background. For example:

```
# If we wanted to run a client
\UnityMirrorTutorials-master\Builds\StandaloneWindows64\NetworkingTutorial.exe

# If we wanted to run a server
\UnityMirrorTutorials-master\Builds\WindowsServer\NetworkingTutorial.exe
```

Now that either the client or server has been started, we can build and run the other normally in Unity Editor. To connect to a locally running server from the client, on the main menu of the game:

1. Select 'Start Client'

_Note: `7777` is the default port for a game server to run on._

## The Backend

Detailed instructions for how to run the backend that powers the game client + game server can be found at `UnityMirrorTutorials-master/README.md`.