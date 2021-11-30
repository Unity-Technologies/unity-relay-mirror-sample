Server Query is how Multiplay monitors game servers that are running. There are two primary protocols that are followed, SQP and A2S.

A spec for SQP can be found alongside this file, "SQP.md". A spec for A2S can be found [here](https://developer.valvesoftware.com/wiki/Server_queries).

Example code for both A2S and SQP can be found in the MirrorSample source. The specific files that pertain to server query are located in `UnityMirrorTutorials-master\Packages\com.unity.multiplayer.online.helpers.sqp\Runtime`. They are:

- `ServerQueryManager.cs`
- `SQPServer.cs`
- `Data\PlayerData.cs`
- `Data\QueryData.cs`
- `Data\QueryDataProvider.cs`
- `Data\RulesData.cs`
- `Data\ServerData`
- `Data\TeamData.cs`
- `Protocols\Serializer.cs`
- `Protocols\A2S\A2SProtocol.cs`
- `Protocols\A2S\A2SCollections.cs`
- `Protocols\IProtocol.cs`
- `Protocols\TF2E\TF2EProtocol.cs`
- `Protocols\TF2E\TF2ECollections.cs`
- `Protocols\SQP\QueryHeader.cs`
- `Protocols\SQP\SQPProtocol.cs`

Arguments that should be passed to your server binary are:

- `-server`
- `-queryport=$$query_port$$`
- `-version=$$game_version$$`
- `-queryprotocol=<a2s|sqp|tf2e>`

## Configuration

The following changes are needed to configure server query:
1) Import the 'com.unity.multiplayer.online.helpers.sqp' package into your project
2) Add the `ServerQueryManager` into your scene
3) Make a call to 'ServerQueryManager.StartServer' with your desired protocol, port, and query data.
    

## Testing Locally

To test locally you can use [qo-a2s](https://github.com/rumblefrog/go-a2s/) for A2S and [go-svrquery](https://github.com/multiplay/go-svrquery) for SQP.

### A2S

To use go-a2s, follow the instructions on the GitHub page for setting up a main within a new project. Switch out calls to client.QueryInfo() with .QueryPlayer/.QueryRules to test Server Info, Server Players, and Server Rule protocols.

After installing go-a2s, you can run a local server by building a windows server and running through command line with the following arguments:
```
<GameName> <MapName> -server -queryport=7778 -version=001 -queryprotocol=a2s
```

Within the main of your go-a2s project, set the local ip to 127.0.0.1 and port to 7778

Open command prompt and navigate to the main.go folder. Run main with the following command

```
go run main.go
```

### SQP

After installing go-svrquery, you can run a local server by building the Development Editor with the following commandline args:
```
<GameName> <MapName> -server -queryport=7778 -version=001 -queryprotocol=sqp
```

The recommended CLI for running queries can be found [here](https://github.com/multiplay/go-svrquery/blob/master/cmd/cli/main.go). Using this CLI, the server may be queried with the following command:
```
go run main.go -addr 127.0.0.1:7778 -proto sqp
```

### TF2E

After installing go-svrquery, you can run a local server by building the Development Editor with the following commandline args:
```
<GameName> <MapName> -server -queryport=7778 -version=001 -queryprotocol=tf2e
```

The recommended CLI for running queries can be found [here](https://github.com/multiplay/go-svrquery/blob/master/cmd/cli/main.go). Using this CLI, the server may be queried with the following command:
```
go run main.go -addr 127.0.0.1:7778 -proto tf2e-v8
```