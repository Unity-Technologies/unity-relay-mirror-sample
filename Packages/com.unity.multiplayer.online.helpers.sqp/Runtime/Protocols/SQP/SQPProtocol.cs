using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using Unity.Helpers.ServerQuery.Data;
using Unity.Helpers.ServerQuery.Protocols.SQP.Collections;
using Unity.ServerQuery.Protocols.SQP;

namespace Unity.Helpers.ServerQuery.Protocols.SQP
{
    public class SQPProtocol : IProtocol
    {
        public static UInt16 SQPVersion => 1;

        private static RNGCryptoServiceProvider s_rng = new RNGCryptoServiceProvider();

        private Dictionary<IPEndPoint, UInt32> m_tokens = new Dictionary<IPEndPoint, uint>();

        public byte[] ReceiveData(byte[] data, IPEndPoint remoteClient, uint serverID)
        {
            //Minimal packet size is five
            if (data.Length < 5) return null;

            //Parse packet header
            Serializer ser = new Serializer(data, Serializer.SerializationMode.Read);
            var header = PacketHeader.Deserialize(ser);

            //The packet is a challenge request
            switch (header.Type)
            {
                //The packet is a challenge request
                case PacketType.Challenge:
                    return BuildChallengeResponse(header, remoteClient);
                //The packet is a query request
                case PacketType.Query:
                    return HandleQueryRequest(header, ser, remoteClient, serverID);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public int GetDefaultPort()
        {
            return 7779;
        }

        private uint GenerateToken(IPEndPoint client)
        {
            byte[] randomToken = new byte[4];
            s_rng.GetBytes(randomToken);
            var token = BitConverter.ToUInt32(randomToken, 0);
            m_tokens.Add(client, token);
            return token;
        }

        private bool CheckToken(PacketHeader header, IPEndPoint client)
        {
            return m_tokens.ContainsKey(client) && m_tokens[client] == header.ChallengeToken;
        }

        private byte[] BuildChallengeResponse(PacketHeader header, IPEndPoint client)
        {
            //Generate a new challenge token
            header.ChallengeToken = GenerateToken(client);
            //Serialize the header (no payload for that)
            byte[] response = new byte[PacketHeader.Size()];
            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);
            header.Serialize(ser);
            //Send it back
            return response;
        }

        private byte[] BuildServerInfoResponse(PacketHeader header, uint serverID)
        {
            var info = QueryDataProvider.GetServerData(serverID).ServerInfo;
            var chunkLength = Serializer.UShortSize + // Current Players
                                  Serializer.UShortSize + // Max players
                                  Serializer.StringSize(info.ServerName) + // Server name
                                  Serializer.StringSize(info.GameType) + // Game mode name
                                  Serializer.StringSize(info.BuildID) + // Build version
                                  Serializer.StringSize(info.Map) + // Map name
                                  Serializer.UShortSize; // Game port
            var packetLength = chunkLength + Serializer.UIntSize; // Length of data chunk following
            int packetSize =
                PacketHeader.Size() + // Packet header 
                Serializer.UShortSize + // SQP Version
                Serializer.ByteSize + // Current Packet
                Serializer.ByteSize + // Last Packet
                Serializer.UShortSize + // Packet Length
                Serializer.UIntSize + // Length of data chunk following
                chunkLength;
            
            byte[] response = new byte[packetSize];

            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);
            //Packet header
            SQPInfoResponsePacket responsePacket = new SQPInfoResponsePacket();
            responsePacket.header = header;

            //SQP version
            responsePacket.version = SQPVersion;

            // Current and last packet
            responsePacket.currentPacket = 0;
            responsePacket.lastPacket = 0;

            //Packet length
            responsePacket.packetLength = (ushort)packetLength;
            //Chunk length
            responsePacket.chunkLength = (ushort)chunkLength;

            //Current players in game
            responsePacket.currentPlayers = (ushort)info.CurrentPlayers;
            //Max player in game
            responsePacket.maxPlayers = (ushort)info.MaxPlayers;

            //Server name
            responsePacket.serverName = info.ServerName;

            //Game type name
            responsePacket.gameType = info.GameType;

            //Build ID
            responsePacket.buildID = info.BuildID;

            //Map name
            responsePacket.map = info.Map;

            //Port number
            responsePacket.gamePort = info.GamePort;

            responsePacket.Serialize(ser);
            
            return response;
        }

        private byte[] BuildServerRuleResponse(PacketHeader header, uint serverID)
        {
            //length of rules chunk
            int chunkLength = 
                Serializer.StringSize("TestRule") + // rule key
                Serializer.ByteSize + // rule type
                Serializer.StringSize("TestValue"); // rule value

            int packetLength = chunkLength + Serializer.UIntSize; // accounting for size of chunkLength field

            int packetSize = 
                PacketHeader.Size() + // Header size
                Serializer.UShortSize + // SQP Version
                Serializer.ByteSize + // Current Packet
                Serializer.ByteSize + // Packet Length
                Serializer.UShortSize + // Packet Length
                packetLength + 1; // Chunk Length + rules

            byte[] response = new byte[packetSize];

            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);

            SQPRulesResponsePacket responsePacket = new SQPRulesResponsePacket();
            responsePacket.header = header;
            responsePacket.version = SQPVersion;
            responsePacket.currentPacket = 0;
            responsePacket.lastPacket = 0;
            responsePacket.packetLength = (ushort)packetLength;
            responsePacket.chunkLength = (uint)chunkLength;

            SQPServerRule rule = new SQPServerRule();
            rule.key = "TestRule";
            rule.type = 4; // string value
            rule.valueString = "TestValue";

            SQPServerRule[] rules = new SQPServerRule[1];
            rules[0] = rule;

            responsePacket.rules = rules;

            responsePacket.Serialize(ser);
            return response;
        }

        private byte[] BuildPlayerInfoResponse(PacketHeader header, uint serverID)
        {
            int chunkLength = 
                Serializer.UShortSize + // Player Count
                Serializer.ByteSize + // Field Count
                Serializer.StringSize("PlayerName") + // Field One - Key
                Serializer.ByteSize + // Field One - Type
                Serializer.StringSize("Jeff") + // Player One - Field Value
                Serializer.StringSize("John"); // Player Two - Field Value

            int packetLength = chunkLength + Serializer.UIntSize;

            int packetSize =
                PacketHeader.Size() + // Header size
                Serializer.UShortSize + // SQP Version
                Serializer.ByteSize + // Current Packet
                Serializer.ByteSize + // Last Packet
                Serializer.UShortSize + // Packet Length
                packetLength + 1; // Chunk Length + rules

            byte[] response = new byte[packetSize];

            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);

            SQPPlayerResponsePacket responsePacket = new SQPPlayerResponsePacket();
            responsePacket.header = header;
            responsePacket.version = SQPVersion;
            responsePacket.currentPacket = 0;
            responsePacket.lastPacket = 0;
            responsePacket.packetLength = (ushort)packetLength;
            responsePacket.chunkLength = (uint)chunkLength;
            responsePacket.playerCount = 2;
            responsePacket.fieldCount = 1;

            SQPFieldKeyValue fieldPlayerOne = new SQPFieldKeyValue();
            fieldPlayerOne.key = "PlayerName";
            fieldPlayerOne.type = 4;
            fieldPlayerOne.valueString = "Jeff";
            SQPFieldKeyValue[] fieldsOne = new SQPFieldKeyValue[1];
            fieldsOne[0] = fieldPlayerOne;

            SQPFieldKeyValue fieldPlayerTwo = new SQPFieldKeyValue();
            fieldPlayerTwo.key = "PlayerName";
            fieldPlayerTwo.type = 4;
            fieldPlayerTwo.valueString = "John";
            SQPFieldKeyValue[] fieldsTwo = new SQPFieldKeyValue[1];
            fieldsTwo[0] = fieldPlayerTwo;

            SQPFieldContainer playerOne = new SQPFieldContainer();
            playerOne.fields = fieldsOne;
            SQPFieldContainer playerTwo = new SQPFieldContainer();
            playerTwo.fields = fieldsTwo;
            SQPFieldContainer[] players = new SQPFieldContainer[2];
            players[0] = playerOne;
            players[1] = playerTwo;

            responsePacket.players = players;

            responsePacket.Serialize(ser);
            
            return response;
        }

        private byte[] BuildTeamInfoResponse(PacketHeader header, uint serverID)
        {
            int chunkLength =
                Serializer.UShortSize + // Team Count
                Serializer.ByteSize + // Field Count
                Serializer.StringSize("Score") + // Field One - Key
                Serializer.ByteSize + // Field One - Type
                Serializer.UIntSize + // Player One - Field Value
                Serializer.UIntSize; // Player Two - Field Value

            int packetLength = chunkLength + Serializer.UIntSize;

            int packetSize =
                PacketHeader.Size() + // Header size
                Serializer.UShortSize + // SQP Version
                Serializer.ByteSize + // Current Packet
                Serializer.ByteSize + // Last Packet
                Serializer.UShortSize + // Packet Length
                packetLength + 1; // Chunk Length + rules

            byte[] response = new byte[packetSize];

            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);

            SQPTeamResponsePacket responsePacket = new SQPTeamResponsePacket();
            responsePacket.header = header;
            responsePacket.version = SQPVersion;
            responsePacket.currentPacket = 0;
            responsePacket.lastPacket = 0;
            responsePacket.packetLength = (ushort)packetLength;
            responsePacket.chunkLength = (uint)chunkLength;
            responsePacket.teamCount = 2;
            responsePacket.fieldCount = 1;

            SQPFieldKeyValue fieldPlayerOne = new SQPFieldKeyValue();
            fieldPlayerOne.key = "Score";
            fieldPlayerOne.type = 2;
            fieldPlayerOne.valueUInt = 23;
            SQPFieldKeyValue[] fieldsOne = new SQPFieldKeyValue[1];
            fieldsOne[0] = fieldPlayerOne;

            SQPFieldKeyValue fieldPlayerTwo = new SQPFieldKeyValue();
            fieldPlayerTwo.key = "Score";
            fieldPlayerTwo.type = 2;
            fieldPlayerTwo.valueUInt = 11;
            SQPFieldKeyValue[] fieldsTwo = new SQPFieldKeyValue[1];
            fieldsTwo[0] = fieldPlayerTwo;

            SQPFieldContainer teamOne = new SQPFieldContainer();
            teamOne.fields = fieldsOne;
            SQPFieldContainer teamTwo = new SQPFieldContainer();
            teamTwo.fields = fieldsTwo;
            SQPFieldContainer[] teams = new SQPFieldContainer[2];
            teams[0] = teamOne;
            teams[1] = teamTwo;
            responsePacket.teams = teams;

            responsePacket.Serialize(ser);

            return response;
        }

        private byte[] HandleQueryRequest(PacketHeader header, Serializer ser, IPEndPoint remoteClient, uint serverID)
        {
            QueryHeader queryHeader = QueryHeader.Deserialize(ser);
            //Invalid SQP version
            if (queryHeader.Version != SQPVersion) return null;
            //Invalid token
            if (!CheckToken(header, remoteClient)) return null;
            //Build and send the answer
            switch (queryHeader.RequestedChunks)
            {
                case QueryType.ServerInfo:
                    return BuildServerInfoResponse(header, serverID);
                case QueryType.ServerRules:
                    return BuildServerRuleResponse(header, serverID);
                case QueryType.PlayerInfo:
                    return BuildPlayerInfoResponse(header, serverID);
                case QueryType.TeamInfo:
                    return BuildTeamInfoResponse(header, serverID);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}