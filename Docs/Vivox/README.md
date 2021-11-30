In-depth Vivox documentation may be found here: https://docs.vivox.com/v5/general/unreal/5_15_0/en-us/Default.htm

**Note: The FPS sample referenced in the documentation on Vivox's Website is not the same codebase as this. This example code was written using that as a reference, but has changes that support running a dedicated server.**

## Example Code

The Vivox component in this project requires running `SampleBackend`. To run the Sample Backend, refer to the `README.md` in the `SampleBackend` folder.

To configure the game client to point towards the backend, you may refer to the `sampleBackendUrl` variable in `UnityRpc.cs`.

The specific files that pertain to Vivox are located in `UnityMirrorTutorials-master\Assets\Scripts`. They are:

- `VivoxManager.cs`

## Configuration

To configure Vivox in your own application, after you've pointed your game client towards the backend you can start integrating the SDK and copy some example code.

1. Update manifest.json to include "com.unity.services.vivox": "15.1.150002-pre.1"

2. In Unity, select Edit->Project Settings->Services and link your project to your UPID

3. In Unity, select Edit->Project Settings->Services->Vivox and ensure the Environment is Automatic

4. Add a Vivox Manager class somewhere in the client scripts. For example Bossroom->Scripts->Client->Game->Vivox->VivoxManager.cs

