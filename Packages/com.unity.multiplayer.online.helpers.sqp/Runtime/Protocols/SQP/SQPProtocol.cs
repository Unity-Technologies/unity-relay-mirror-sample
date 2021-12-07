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
            var header = SQPPacketHeader.Deserialize(ser);

            //The packet is a challenge request
            switch (header.Type)
            {
                //The packet is a challenge request
                case PacketType.Challenge:
                    return BuildChallengeResponse(header, remoteClient);
                //The packet is a query request
                case PacketType.Query:
                    return SendQueryRequest(header, ser, remoteClient, serverID);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private byte[] SendQueryRequest(SQPPacketHeader header, Serializer serRead, IPEndPoint remoteClient, uint serverID)
        {
            var info = QueryDataProvider.GetServerData(serverID).ServerInfo;
            QueryHeader queryHeader = QueryHeader.Deserialize(serRead);
            //Invalid SQP version
            if (queryHeader.Version != SQPVersion) return null;
            //Invalid token
            if (!CheckToken(header, remoteClient)) return null;
            //Build and send the answer

            int headerSize =
                SQPPacketHeader.Size() + // Packet header 
                Serializer.UShortSize + // SQP Version
                Serializer.ByteSize + // Current Packet
                Serializer.ByteSize + // Last Packet
                Serializer.UShortSize; // Packet Length

            int packetSize = 0;

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.ServerInfo) > 0)
            {
                packetSize += Serializer.UIntSize + // Size of ServerInfoChunckLength
                    Serializer.UShortSize + // Current Players
                    Serializer.UShortSize + // Max Players
                    Serializer.StringSize(info.ServerName) + // Server Name
                    Serializer.StringSize(info.GameType) + // Game Type
                    Serializer.StringSize(info.BuildID) + // Build ID
                    Serializer.StringSize(info.Map) + // Map
                    Serializer.UShortSize; // Game Port
            }

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.ServerRules) > 0)
            {
                packetSize += Serializer.UIntSize + // Size of rulesChunkLength
                    Serializer.StringSize("TestRule") + // rule key
                    Serializer.ByteSize + // rule type
                    Serializer.StringSize("TestValue") + 1; // rule value
            }

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.PlayerInfo) > 0)
            {
                packetSize += Serializer.UIntSize + // Size of playersChunkLength
                    Serializer.UShortSize + // Player Count
                    Serializer.ByteSize + // Field Count
                    Serializer.StringSize("PlayerName") + // Field One - Key
                    Serializer.ByteSize + // Field One - Type
                    Serializer.StringSize("Jeff") + // Player One - Field Value
                    Serializer.StringSize("John") + 1; // Player Two - Field Value
            }

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.TeamInfo) > 0)
            {
                packetSize +=
                    Serializer.UIntSize + // Size of teamInfoChunkLength
                    Serializer.UShortSize + // Team count
                    Serializer.ByteSize + // Field count
                    Serializer.StringSize("Score") + // Field One - Key
                    Serializer.ByteSize + // Field One - Type
                    Serializer.UIntSize + // Player One - Field Value
                    Serializer.UIntSize + 1; // Player Two - Field Value
            }

            byte[] response = new byte[packetSize + headerSize];

            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);

            SQPQueryResponsePacket responsePacket = new SQPQueryResponsePacket();
            responsePacket.requestedChunks = (byte)queryHeader.RequestedChunks;
            responsePacket.header = header;
            responsePacket.CurrentPacket = 0;
            responsePacket.lastPacket = 0;
            responsePacket.version = SQPVersion;
            responsePacket.packetLength = (ushort)packetSize;

            if(((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.ServerInfo) > 0)
            {
                responsePacket.serverInfoData.currentPlayers = (ushort)info.CurrentPlayers;
                responsePacket.serverInfoData.maxPlayers = (ushort)info.MaxPlayers;
                responsePacket.serverInfoData.serverName = info.ServerName;
                responsePacket.serverInfoData.gameType = info.GameType;
                responsePacket.serverInfoData.buildID = info.BuildID;
                responsePacket.serverInfoData.map = info.Map;
                responsePacket.serverInfoData.gamePort = info.GamePort;
            }

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.ServerRules) > 0)
            {
                SQPServerRule rule = new SQPServerRule();
                rule.key = "TestRule";
                rule.type = (byte)SQPDynamicType.String;
                rule.valueString = "TestValue";

                SQPServerRule[] rules = new SQPServerRule[1];
                rules[0] = rule;
                responsePacket.serverRulesData.rules = rules;
            }

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.PlayerInfo)> 0)
            {
                responsePacket.playerInfodata.playerCount = 2;
                responsePacket.playerInfodata.fieldCount = 1;

                SQPFieldKeyValue fieldPlayerOne = new SQPFieldKeyValue();
                fieldPlayerOne.key = "PlayerName";
                fieldPlayerOne.type = (byte)SQPDynamicType.String;
                fieldPlayerOne.valueString = "Jeff";
                SQPFieldKeyValue[] fieldsOne = new SQPFieldKeyValue[1];
                fieldsOne[0] = fieldPlayerOne;

                SQPFieldKeyValue fieldPlayerTwo = new SQPFieldKeyValue();
                fieldPlayerTwo.key = "PlayerName";
                fieldPlayerTwo.type = (byte)SQPDynamicType.String;
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

                responsePacket.playerInfodata.players = players;
            }

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.TeamInfo) > 0)
            {
                responsePacket.teamInfoData.teamCount = 2;
                responsePacket.teamInfoData.fieldCount = 1;

                SQPFieldKeyValue fieldPlayerOne = new SQPFieldKeyValue();
                fieldPlayerOne.key = "Score";
                fieldPlayerOne.type = (byte)SQPDynamicType.Uint;
                fieldPlayerOne.valueUInt = 23;
                SQPFieldKeyValue[] fieldsOne = new SQPFieldKeyValue[1];
                fieldsOne[0] = fieldPlayerOne;

                SQPFieldKeyValue fieldPlayerTwo = new SQPFieldKeyValue();
                fieldPlayerTwo.key = "Score";
                fieldPlayerTwo.type = (byte)SQPDynamicType.Uint;
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

                responsePacket.teamInfoData.teams = teams;
            }

            responsePacket.Serialize(ser);

            return response;
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

        private bool CheckToken(SQPPacketHeader header, IPEndPoint client)
        {
            return m_tokens.ContainsKey(client) && m_tokens[client] == header.ChallengeToken;
        }

        private byte[] BuildChallengeResponse(SQPPacketHeader header, IPEndPoint client)
        {
            //Generate a new challenge token
            header.ChallengeToken = GenerateToken(client);
            //Serialize the header (no payload for that)
            byte[] response = new byte[SQPPacketHeader.Size()];
            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);
            header.Serialize(ser);
            //Send it back
            return response;
        }
    }
}