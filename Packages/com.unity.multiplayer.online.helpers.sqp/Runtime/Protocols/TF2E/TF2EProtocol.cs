using UnityEngine;
using System.Net;
using Unity.Helpers.ServerQuery.Protocols.TF2E.Collections;

namespace Unity.Helpers.ServerQuery.Protocols.TF2E
{
    public class TF2EProtocol : IProtocol
    {
        public byte[] ReceiveData(byte[] data, IPEndPoint remoteClient, uint serverID)
        {
            //Parse packet header
            Serializer serializer = new Serializer(data, Serializer.SerializationMode.Read);
            var requestPacket = TF2ERequestPacket.Deserialize(serializer);

            if(requestPacket.requestType == (byte)TF2EMessageType.ServerInfoRequest)
            {
                int packetSize =
                Serializer.IntSize + // header prefix
                Serializer.ByteSize + // header command
                Serializer.ByteSize + // header version
                Serializer.ByteSize + // instance info - retail
                Serializer.ByteSize + // instance info - instance type
                Serializer.UIntSize + // instance info - clientCRC
                Serializer.UShortSize + // instance info - netProtocol
                Serializer.UIntSize + // instance info - health flags
                Serializer.UIntSize + // instance info - random server id
                Serializer.StringSize("Test Build") + // build name
                Serializer.StringSize("Test Datacenter") + // data center
                Serializer.StringSize("Test Gamemode") + // game mode
                Serializer.UShortSize + // basic info - port
                Serializer.StringSize("PC") + // basic info - platform
                Serializer.StringSize("N/A") + // basic info - playlist version
                Serializer.UIntSize + // basic info - playlist number
                Serializer.StringSize("Team Deathmatch") + // basic info - playlist name
                Serializer.ByteSize + // platform num
                Serializer.StringSize("PC") + // basic info - platform players, first platform
                Serializer.ByteSize + // basic info - platform players, first platform player count
                Serializer.ByteSize + //basic info - num clients
                Serializer.ByteSize + // basic info - max clients
                Serializer.StringSize("Highrise") +
                Serializer.FloatSize + // performance info - average frame time 
                Serializer.FloatSize + // performance info - max frame time 
                Serializer.FloatSize + // performance info - average user command time
                Serializer.FloatSize + // performance info - max user command time
                Serializer.ByteSize + // match state - phase 
                Serializer.ByteSize + // match state - max rounds
                Serializer.ByteSize + // match state - rounds won IMC
                Serializer.ByteSize + // match state - rounds won Militia
                Serializer.UShortSize + // match state - time limit in seconds
                Serializer.UShortSize + // match state - time passed in seconds
                Serializer.UShortSize + // match state - max score
                Serializer.UShortSize + // match state - teams left with player numbers
                Serializer.ByteSize + // team one - id
                Serializer.UShortSize + // team one - score
                Serializer.ByteSize + // team two - id
                Serializer.UShortSize + // team two - score
                Serializer.ByteSize + // teams terminator
                Serializer.ULongSize + // client one - id
                Serializer.StringSize("Titanfall") + // client one - name
                Serializer.ByteSize + // client one - teamID
                Serializer.StringSize("127.0.0.1") + // client one - address
                Serializer.UIntSize + // client one - ping
                Serializer.UIntSize + // client one - packets Received
                Serializer.UIntSize + // client one - packets dropped
                Serializer.UIntSize + // client one - score
                Serializer.UShortSize + // client one - kills
                Serializer.UShortSize + // client one - deaths
                Serializer.ULongSize + // client two - id
                Serializer.StringSize("Titangebackup") + // client two - name
                Serializer.ByteSize + // client two - teamID
                Serializer.StringSize("127.0.0.1") + // client two - address
                Serializer.UIntSize + // client two - ping
                Serializer.UIntSize + // client two - packets received
                Serializer.UIntSize + // client two - packets dropped
                Serializer.UIntSize + // client two - score
                Serializer.UShortSize + // client two - kills
                Serializer.UShortSize + // client two - deaths
                Serializer.ULongSize; // client terminator 

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

                responsePacket.buildName = "Test Build";
                responsePacket.dataCenter = "Test Datacenter";
                responsePacket.gameMode = "Test Gamemode";

                responsePacket.basicInfo.port = 7777;
                responsePacket.basicInfo.platform = "PC";
                responsePacket.basicInfo.playlistVersion = "N/A";
                responsePacket.basicInfo.playlistNum = 0;
                responsePacket.basicInfo.playlistName = "Team Deathmatch";
                responsePacket.basicInfo.numClients = 0;
                responsePacket.basicInfo.maxClients = 16;
                responsePacket.basicInfo.map = "Highrise";

                string platformOne = "PC";
                byte platformOnePlayerCount = 2;
                responsePacket.basicInfo.platformPlayers.Add(platformOne, platformOnePlayerCount);

                responsePacket.performanceInfo.averageFrameTime = 1.0f;
                responsePacket.performanceInfo.maxFrameTime = 2.0f;
                responsePacket.performanceInfo.averageUserCommandTime = 3.0f;
                responsePacket.performanceInfo.maxUserCommandTime = 4.0f;

                responsePacket.matchState.phase = 0;
                responsePacket.matchState.maxRounds = 3;
                responsePacket.matchState.roundsWonIMC = 0;
                responsePacket.matchState.roundsWonMilitia = 1;
                responsePacket.matchState.timeLimitSeconds = 60;
                responsePacket.matchState.timePassedSeconds = 5;
                responsePacket.matchState.maxScore = 20;
                responsePacket.matchState.teamsLeftWithPlayersNum = 2;

                TF2ETeam teamOne = new TF2ETeam();
                teamOne.id = 1;
                teamOne.score = 5;

                TF2ETeam teamTwo = new TF2ETeam();
                teamTwo.id = 2;
                teamTwo.score = 6;

                TF2ETeam[] teams = new TF2ETeam[2];
                teams[0] = teamOne;
                teams[1] = teamTwo;
                responsePacket.teams = teams;

                TF2EClient clientOne = new TF2EClient();
                clientOne.id = 1;
                clientOne.name = "Titanfall";
                clientOne.teamID = teamOne.id;
                clientOne.address = "127.0.0.1";
                clientOne.ping = 30;
                clientOne.packetsReceived = 100;
                clientOne.packetsDropped = 5;
                clientOne.score = 10;
                clientOne.kills = 11;
                clientOne.deaths = 1;

                TF2EClient clientTwo = new TF2EClient();
                clientTwo.id = 2;
                clientTwo.name = "Titangebackup";
                clientTwo.teamID = teamTwo.id;
                clientTwo.address = "127.0.0.1";
                clientTwo.ping = 40;
                clientTwo.packetsReceived = 105;
                clientTwo.packetsDropped = 10;
                clientTwo.score = 1;
                clientTwo.kills = 1;
                clientTwo.deaths = 10;

                TF2EClient[] clients = new TF2EClient[2];
                clients[0] = clientOne;
                clients[1] = clientTwo;
                responsePacket.clients = clients;

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