The SampleBackend is in charge of managing a user's identity, generating Vivox tokens, and interacting with the Unity Matchmaker service. It currently also supports managing manual allocations against Multiplay via a dashboard that can be located by navigating to the base URL for your backend. I.e. if running locally: http://localhost:8080/

In order to configure the backend for your game, refer to the `config-example.toml` file to create a `config.toml` file. You will need to add your own service identifiers received during onboarding for each Unity service.

## Game Client

As it pertains to the game client, there is one class that is made to communicate with the backend. It is located in `UnityMirrorTutorials-master\Assets\Scripts`:

- `UnityRpc.cs`

To configure the game client to point towards the backend, change the localBackendUrl variable within the UnityRpc.cs file