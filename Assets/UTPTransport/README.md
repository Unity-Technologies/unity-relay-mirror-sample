# UTP Transport for Mirror

This is the [Unity Transport Package](https://docs.unity3d.com/Packages/com.unity.transport@1.0/manual/index.html) (UTP) transport for [Mirror](https://github.com/vis2k/Mirror). It is actively in development and is not considered stable.

## Dependencies
- Mirror ([Documentation](https://mirror-networking.gitbook.io/docs/))
- UTP ([Documentation](https://docs.unity3d.com/Packages/com.unity.transport@1.0/manual/index.html))

## Installation
1. Visit the [Mirror Asset Store Page](https://assetstore.unity.com/packages/tools/network/mirror-129321) and add Mirror to "My Assets"
2. Import Mirror with Package Manager (Window -> Package Manager -> Packages: My Assets -> Mirror -> Download/Import)
3. Import UTP with Package Manager (Window -> Package Manager -> Add -> Add package from git URL... -> "com.unity.transport@1.0.0-pre.10")
4. Copy the directory that this README is in to your "Assets" folder
5. Attach a "NetworkManager" to a "GameObject" in your "Scene"
6. Attach a "UtpTransport" component to the same "GameObject" as your "NetworkManager"
7. Select the "UtpTransport" as the transport for your "NetworkManager"