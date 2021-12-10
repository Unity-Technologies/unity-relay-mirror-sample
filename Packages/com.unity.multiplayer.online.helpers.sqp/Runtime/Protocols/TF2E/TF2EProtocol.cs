using System.Net;
using UnityEngine;
using System.Collections.Generic;
using Unity.Helpers.ServerQuery.Data;
using Unity.Helpers.ServerQuery.Protocols.TF2E.Collections;

namespace Unity.Helpers.ServerQuery.Protocols.TF2E
{
    public class TF2EProtocol : IProtocol
    {
        public byte[] ReceiveData(byte[] data, IPEndPoint remoteClient, uint serverID)
        {
            var info = QueryDataProvider.GetServerData(serverID).TF2EQueryInfo;
            // Parse packet header
            Serializer serializer = new Serializer(data, Serializer.SerializationMode.Read);
            var requestPacket = TF2ERequestPacket.Deserialize(serializer);

            if(requestPacket.requestType == (byte)TF2EMessageType.ServerInfoRequest)
            {
                int packetSize =
                Serializer.IntSize + // Header prefix
                Serializer.ByteSize + // Header command
                Serializer.ByteSize + // Header version
                Serializer.ByteSize + // Instance info - retail
                Serializer.ByteSize + // Instance info - instance type
                Serializer.UIntSize + // Instance info - clientCRC
                Serializer.UShortSize + // Instance info - netProtocol
                Serializer.UIntSize + // Instance info - health flags
                Serializer.UIntSize + // Instance info - random server id
                Serializer.StringSize(info.buildName) + // Build name
                Serializer.StringSize(info.dataCenter) + // Data center
                Serializer.StringSize(info.gameMode) + // Game mode
                Serializer.UShortSize + // Basic info - Port
                Serializer.StringSize(info.basicInfo.platform) + // Basic info - Platform
                Serializer.StringSize(info.basicInfo.playlistVersion) + // Basic info - Playlist version
                Serializer.UIntSize + // Basic info - Playlist number
                Serializer.StringSize(info.basicInfo.playlistName) + // Basic info - Playlist name
                Serializer.ByteSize; // Basic info - Platform num

                foreach(KeyValuePair<string, byte> platform in info.basicInfo.platformPlayers)
                {
                    packetSize +=
                        Serializer.StringSize(platform.Key) + // Basic Info - Platform
                        Serializer.ByteSize; // Basic info - Platform player count
                }

                packetSize +=
                    Serializer.ByteSize + // Basic info - Num clients
                    Serializer.ByteSize + // Basic info - Max clients
                    Serializer.StringSize(info.basicInfo.map) + // Basic info - Map
                    Serializer.FloatSize + // Performance info - Average frame time 
                    Serializer.FloatSize + // Performance info - Max frame time 
                    Serializer.FloatSize + // Performance info - Average user command time
                    Serializer.FloatSize + // Performance info - Max user command time
                    Serializer.ByteSize + // Match state - Phase 
                    Serializer.ByteSize + // Match state - Max rounds
                    Serializer.ByteSize + // Match state - Rounds won IMC
                    Serializer.ByteSize + // Match state - Rounds won Militia
                    Serializer.UShortSize + // Match state - Time limit in seconds
                    Serializer.UShortSize + // Match state - Time passed in seconds
                    Serializer.UShortSize + // Match state - Max score
                    Serializer.UShortSize; // Match state - Teams left with player numbers

                foreach(TF2ETeam team in info.teams)
                {
                    packetSize +=
                        Serializer.ByteSize + // Team - ID
                        Serializer.UShortSize; // Team - Score
                }

                packetSize += Serializer.ByteSize; // teams terminator

                foreach(TF2EClient client in info.clients)
                {
                    packetSize +=
                        Serializer.ULongSize + // Client - ID
                        Serializer.StringSize(client.name) + // Client - Name
                        Serializer.ByteSize + // Client - TeamID
                        Serializer.StringSize(client.address) + // Client - Address
                        Serializer.UIntSize + // Client - Ping
                        Serializer.UIntSize + // Client - Packets Received
                        Serializer.UIntSize + // Client - Packets dropped
                        Serializer.UIntSize + // Client - Score
                        Serializer.UShortSize + // Client - Kills
                        Serializer.UShortSize; // Client - Deaths

                }

                packetSize +=
                    Serializer.ULongSize + 16; // client terminator 

                byte[] response = new byte[packetSize];

                Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);

                TF2EResponsePacket responsePacket = new TF2EResponsePacket();
                responsePacket.Header.prefix = -1;
                responsePacket.Header.command = (byte)TF2EMessageType.ServerInfoResponse;
                responsePacket.Header.version = requestPacket.version;

                responsePacket.instanceInfo.retail = 1;
                responsePacket.instanceInfo.instanceType = 2;
                responsePacket.instanceInfo.clientCRC = 0;
                responsePacket.instanceInfo.netProtocol = 0;
                responsePacket.instanceInfo.healthFlags = (int)TF2EHealthFlags.PacketLossIn | (int)TF2EHealthFlags.PacketLossOut | (int)TF2EHealthFlags.Hitching;
                responsePacket.instanceInfo.randomServerId = 1;

                responsePacket.buildName = info.buildName;
                responsePacket.dataCenter = info.dataCenter;
                responsePacket.gameMode = info.gameMode;

                responsePacket.basicInfo.port = info.basicInfo.port;
                responsePacket.basicInfo.platform = info.basicInfo.platform;
                responsePacket.basicInfo.playlistVersion = info.basicInfo.playlistVersion;
                responsePacket.basicInfo.playlistNum = info.basicInfo.playlistNum;
                responsePacket.basicInfo.playlistName = info.basicInfo.playlistName;
                responsePacket.basicInfo.numClients = info.basicInfo.numClients;
                responsePacket.basicInfo.maxClients = info.basicInfo.maxClients;
                responsePacket.basicInfo.map = info.basicInfo.map;

                responsePacket.basicInfo.platformPlayers = info.basicInfo.platformPlayers;

                responsePacket.performanceInfo.averageFrameTime = info.performanceInfo.averageFrameTime;
                responsePacket.performanceInfo.maxFrameTime = info.performanceInfo.maxFrameTime;
                responsePacket.performanceInfo.averageUserCommandTime = info.performanceInfo.averageUserCommandTime;
                responsePacket.performanceInfo.maxUserCommandTime = info.performanceInfo.maxUserCommandTime;

                responsePacket.matchState.phase = info.matchState.phase;
                responsePacket.matchState.maxRounds = info.matchState.maxRounds;
                responsePacket.matchState.roundsWonIMC = info.matchState.roundsWonIMC;
                responsePacket.matchState.roundsWonMilitia = info.matchState.roundsWonMilitia;
                responsePacket.matchState.timeLimitSeconds = info.matchState.timeLimitSeconds;
                responsePacket.matchState.timePassedSeconds = info.matchState.timePassedSeconds;
                responsePacket.matchState.maxScore = info.matchState.maxScore;
                responsePacket.matchState.teamsLeftWithPlayersNum = info.matchState.teamsLeftWithPlayersNum;

                responsePacket.teams = info.teams;

                responsePacket.clients = info.clients;

                responsePacket.Serialize(ser);

                return response;
            }

            Debug.Log("invalid request type found");
            return null;
        }
        public int GetDefaultPort()
        {
            return 7779;
        }
    }
}