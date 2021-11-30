In-depth Multiplay documentation may be found here: https://docs.unity.com/multiplay/shared/welcome-to-multiplay.htm

## Game Server Deployment

Docs + example code for game server deployment to Multiplay can be found in "Deployment" folder. Your Multiplay/Unity representative should refer to the "Notes for Unity Representative -> Multiplay" section at the bottom of this file for configuration information specific to UE4.

## Server Query

Instructions and references to example code for Multiplay Server Query can be found in the "Server Query" folder.

## Example Code

There is example code for allocating, polling, and deallocating servers in the `SampleBackend` code base. There is a dashboard that manages all of these functions. Once you have an IP and a port from the dashboard, you may travel to it from the main menu in the game by:

1. N/A yet

If you would like to test a more end-to-end example of Multiplay, you may do so by selecting "Create A Match" on the main menu. This will trigger a request to the `SampleBackend` to allocate a server. The game client will then begin to poll the backend. Once the server is ready, the backend will respond with an IP/Port. The game client will then travel to this IP/Port.

The specific files that pertain to Multiplay are located in `UnityMirrorTutorials-master\Assets\Scripts`. They are:

- `UnityRpc.cs`
- `MyNetworkManager.cs`

## Notes for Unity Representative

If you are a Unity Representative that is spinning this client up here are a few notes that are helpful to configuring UE4 games modeled after this sample.

A good starting point for UE4 games regarding an INIT config is the following:

```
PARAMS=' \
$$cliparams$$ \
-userdir=../$$ConfigPathRelative$$ \
-log=$$serverid$$.log \
-port=$$port$$ \
-queryport=$$queryport$$ \
-queryprotocol=<a2s|sqp> \
-version=$$game_version$$
'
```

_Note:_ Only one query protocol should be specified, it will depend on the query protocol type that the mod supports.

UE4 profiles should also have a field called `cliparams` that the customer is able to edit. This is where they'll place map/game mode information specific to that profile.