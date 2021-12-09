using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using Unity.Helpers.ServerQuery.Data;
using Unity.Helpers.ServerQuery.Protocols.SQP.Collections;
using Unity.ServerQuery.Protocols.SQP;
using UnityEngine;

namespace Unity.Helpers.ServerQuery.Protocols.SQP
{
    public class SQPProtocol : IProtocol
    {
        public static UInt16 SQPVersion => 1;

        private static RNGCryptoServiceProvider s_rng = new RNGCryptoServiceProvider();

        private Dictionary<IPEndPoint, UInt32> m_tokens = new Dictionary<IPEndPoint, uint>();

        public byte[] ReceiveData(byte[] data, IPEndPoint remoteClient, uint serverID)
        {
            // Minimal packet size is five
            if (data.Length < 5) return null;

            // Parse packet header
            Serializer ser = new Serializer(data, Serializer.SerializationMode.Read);
            var header = SQPPacketHeader.Deserialize(ser);

            // The packet is a challenge request
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
            var info = QueryDataProvider.GetServerData(serverID);
            QueryHeader queryHeader = QueryHeader.Deserialize(serRead);
            // Invalid SQP version
            if (queryHeader.Version != SQPVersion) return null;
            // Invalid token
            if (!CheckToken(header, remoteClient)) return null;
            // Build and send the answer

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
                    Serializer.StringSize(info.SQPServerInfo.serverName) + // Server Name
                    Serializer.StringSize(info.SQPServerInfo.gameType) + // Game Type
                    Serializer.StringSize(info.SQPServerInfo.buildID) + // Build ID
                    Serializer.StringSize(info.SQPServerInfo.map) + // Map
                    Serializer.UShortSize; // Game Port
            }

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.ServerRules) > 0)
            {
                packetSize += Serializer.UIntSize + 1; // Size of rulesChunkLength + offset

                // Count packet size based on each rule
                foreach (SQPServerRule rule in info.SQPServerRules.rules)
                {
                    packetSize += Serializer.StringSize(rule.key) + // Rule Key
                        Serializer.ByteSize; // Rule Type

                    switch (rule.type)
                    {
                        case (byte)SQPDynamicType.Byte:
                            packetSize += Serializer.ByteSize;
                            break;
                        case (byte)SQPDynamicType.Ushort:
                            packetSize += Serializer.UShortSize;
                            break;
                        case (byte)SQPDynamicType.Uint:
                            packetSize += Serializer.UIntSize;
                            break;
                        case (byte)SQPDynamicType.Ulong:
                            packetSize += Serializer.ULongSize;
                            break;
                        case (byte)SQPDynamicType.String:
                            packetSize += Serializer.StringSize(rule.valueString);
                            break;
                        default:
                            Debug.Log("invalid SQP server rule type found");
                            break;
                    }
                }
            }

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.PlayerInfo) > 0)
            {

                packetSize += Serializer.UIntSize + // Size of playersChunkLength
                    Serializer.UShortSize + // Player Count
                    Serializer.ByteSize + 1; // Field Count

                // Count up packet size based on the key/type of every field for the first player
                // Every player should have the same fields and every field only needs to print once
                if(info.SQPPlayerInfo.players.Count != 0)
                {
                    foreach (SQPFieldKeyValue field in info.SQPPlayerInfo.players[0].fields)
                    {
                        packetSize += Serializer.StringSize(field.key) + // Field Key
                            Serializer.ByteSize; // Field Type
                    }

                    foreach (SQPFieldContainer player in info.SQPPlayerInfo.players)
                    {
                        foreach (SQPFieldKeyValue field in player.fields)
                        {
                            switch (field.type)
                            {
                                case (byte)SQPDynamicType.Byte:
                                    packetSize += Serializer.ByteSize;
                                    break;
                                case (byte)SQPDynamicType.Ushort:
                                    packetSize += Serializer.UShortSize;
                                    break;
                                case (byte)SQPDynamicType.Uint:
                                    packetSize += Serializer.UIntSize;
                                    break;
                                case (byte)SQPDynamicType.Ulong:
                                    packetSize += Serializer.ULongSize;
                                    break;
                                case (byte)SQPDynamicType.String:
                                    packetSize += Serializer.StringSize(field.valueString);
                                    break;
                                default:
                                    Debug.Log("invalid SQP server rule type found");
                                    break;
                            }
                        }
                    }
                }
            }

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.TeamInfo) > 0)
            {
                packetSize +=
                    Serializer.UIntSize + // Size of teamInfoChunkLength
                    Serializer.UShortSize + // Team count
                    Serializer.ByteSize + 1; // Field count

                if (info.SQPTeamInfo.teams.Count != 0)
                {
                    foreach (SQPFieldKeyValue field in info.SQPTeamInfo.teams[0].fields)
                    {
                        packetSize += Serializer.StringSize(field.key) + // Field key
                            Serializer.ByteSize; // Field type
                    }

                    foreach (SQPFieldContainer team in info.SQPTeamInfo.teams)
                    {
                        foreach (SQPFieldKeyValue field in team.fields)
                        {
                            switch (field.type)
                            {
                                case (byte)SQPDynamicType.Byte:
                                    packetSize += Serializer.ByteSize;
                                    break;
                                case (byte)SQPDynamicType.Ushort:
                                    packetSize += Serializer.UShortSize;
                                    break;
                                case (byte)SQPDynamicType.Uint:
                                    packetSize += Serializer.UIntSize;
                                    break;
                                case (byte)SQPDynamicType.Ulong:
                                    packetSize += Serializer.ULongSize;
                                    break;
                                case (byte)SQPDynamicType.String:
                                    packetSize += Serializer.StringSize(field.valueString);
                                    break;
                                default:
                                    Debug.Log("invalid SQP server rule type found");
                                    break;
                            }
                        }
                    }
                }
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
                responsePacket.serverInfoData.currentPlayers = (ushort)info.SQPServerInfo.currentPlayers;
                responsePacket.serverInfoData.maxPlayers = (ushort)info.SQPServerInfo.maxPlayers;
                responsePacket.serverInfoData.serverName = info.SQPServerInfo.serverName;
                responsePacket.serverInfoData.gameType = info.SQPServerInfo.gameType;
                responsePacket.serverInfoData.buildID = info.SQPServerInfo.buildID;
                responsePacket.serverInfoData.map = info.SQPServerInfo.map;
                responsePacket.serverInfoData.gamePort = info.SQPServerInfo.gamePort;
            }

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.ServerRules) > 0)
            {
                responsePacket.serverRulesData.rules = info.SQPServerRules.rules;
            }

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.PlayerInfo)> 0)
            {
                responsePacket.playerInfodata.playerCount = info.SQPPlayerInfo.playerCount;
                responsePacket.playerInfodata.fieldCount = info.SQPPlayerInfo.fieldCount;
                responsePacket.playerInfodata.players = info.SQPPlayerInfo.players;
            }

            if (((byte)queryHeader.RequestedChunks & (byte)SQPChunkType.TeamInfo) > 0)
            {
                responsePacket.teamInfoData.teamCount = info.SQPTeamInfo.teamCount;
                responsePacket.teamInfoData.fieldCount = info.SQPTeamInfo.fieldCount;
                responsePacket.teamInfoData.teams = info.SQPTeamInfo.teams;
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