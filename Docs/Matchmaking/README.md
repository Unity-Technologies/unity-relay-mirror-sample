In-depth Matchmaking documentation may be found here: https://unity-technologies.github.io/ucg-matchmaking-docs/overview

## Example Code

The Matchmaker component in this project requires a running `SampleBackend`. To run the Sample Backend, refer to the `README.md` in the `UnityMirrorTutorials-master` folder.

To configure the game client to point towards the backend, you may refer to the `localBackendUrl` variable in `UnityRpc.cs`.

You will also need to update the `ticketEndpoint` in `UnityRpc.cs` to point towards your own matchmaking UPID.

The specific files that pertain to matchmaking are located in `UnityMirrorTutorials-master\Assets\Scripts`. They are:

- `UnityRpc.cs`
- `MyNetworkManager.cs`

## Configuration

The following changes are needed to configure matchmaking:
1) Add the UnityRpc.cs file to your project
2) Update the config.toml file for the backend with your multiplay credentials

## Notes

Our example code assumes four different types of matchmaking:

1. Training (illustrates matchmaking into a server alone)
2. Team deathmatch
3. Free for all
4. Random (randomly picks a matchmaking mode)

Example functions for each of these matchmaking types that are to be registered with the Unity Matchmaking service can be found in the `GameModeConfigs/` folder.