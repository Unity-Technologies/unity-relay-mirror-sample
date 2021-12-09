using System;
using UnityEngine;
using System.Collections.Generic;

namespace Unity.Helpers.ServerQuery.Protocols.SQP.Collections
{
    enum SQPChunkType
    {
        ServerInfo = 1 << 0,
		ServerRules = 1 << 1,
		PlayerInfo = 1 << 2,
		TeamInfo = 1 << 3
	}

    public enum PacketType
    {
        Challenge,
        Query
    }

    public enum SQPDynamicType
    {
        Byte = 0,
		Ushort = 1,
		Uint = 2,
		Ulong = 3,
		String = 4
	}

    public struct SQPPacketHeader
    {
        public PacketType Type;
        public UInt32 ChallengeToken;

        public int Serialize(Serializer ser)
        {
            ser.WriteByte((Type == PacketType.Challenge) ? (byte)PacketType.Challenge : (byte)PacketType.Query);
            ser.WriteUInt(ChallengeToken);
            return Size();
        }

        public static SQPPacketHeader Deserialize(Serializer ser)
        {
            var header = new SQPPacketHeader
            {
                Type = (ser.ReadByte() == 0) ? PacketType.Challenge : PacketType.Query,
                ChallengeToken = ser.ReadUIntSQP()
            };
            return header;
        }

        public static int Size()
        {
            return 1 + 4;
        }
    }

    public class SQPServerRule
    {
        public string key;
        public byte type; // 0 for byte, 1 for ushort, 2 for uint 3 for ulong, and 4 for string
        public byte valueByte;
        public ushort valueUShort;
        public uint valueUInt;
        public ulong valueULong;
        public string valueString;

        public SQPServerRule() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteStringSQP(key);
            ser.WriteByte(type);
            switch (type)
            {
                case (byte)SQPDynamicType.Byte:
                    ser.WriteByte(valueByte);
                    break;
                case (byte)SQPDynamicType.Ushort:
                    ser.WriteUShortSQP(valueUShort);
                    break;
                case (byte)SQPDynamicType.Uint:
                    ser.WriteUIntSQP(valueUInt);
                    break;
                case (byte)SQPDynamicType.Ulong:
                    ser.WriteULongSQP(valueULong);
                    break;
                case (byte)SQPDynamicType.String:
                    ser.WriteStringSQP(valueString);
                    break;
                default:
                    Debug.Log("invalid SQP server rule type found");
                    break;
            }
            return 0;
        }
    }

    public class SQPFieldKeyValue
    {
        public string key;
        public byte type;
        public byte valueByte;
        public ushort valueUShort;
        public uint valueUInt;
        public ulong valueULong;
        public string valueString;
    }

    public class SQPFieldContainer
    {
        public List<SQPFieldKeyValue> fields;
    }

    public class SQPInfoResponsePacket
    {
        public ushort currentPlayers;
        public ushort maxPlayers;
        public string serverName;
        public string gameType;
        public string buildID;
        public string map;
        public ushort gamePort;

        public SQPInfoResponsePacket() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteUShortSQP(currentPlayers);
            ser.WriteUShortSQP(maxPlayers);
            ser.WriteStringSQP(serverName);
            ser.WriteStringSQP(gameType);
            ser.WriteStringSQP(buildID);
            ser.WriteStringSQP(map);
            ser.WriteUShortSQP(gamePort);

            return 0;
        }
    }

    public class SQPRulesResponsePacket
    {
        public List<SQPServerRule> rules;

        public SQPRulesResponsePacket() { }

        public int Serialize(Serializer ser)
        {
            foreach(SQPServerRule rule in rules)
            {
                rule.Serialize(ser);
            }

            return 0;
        }
    }

    public class SQPPlayerResponsePacket
    {
        public ushort playerCount;
        public byte fieldCount;
        public List<SQPFieldContainer> players;

        public SQPPlayerResponsePacket() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteUShortSQP(playerCount);
            ser.WriteByte(fieldCount);

            if (players.Count > 0)
            {
                for (int i = 0; i < players[0].fields.Count; i++)
                {
                    ser.WriteStringSQP(players[0].fields[i].key);
                    ser.WriteByte(players[0].fields[i].type);
                }

                for (int i = 0; i < players.Count; i++)
                {
                    for (int j = 0; j < players[i].fields.Count; j++)
                    {
                        switch (players[i].fields[j].type)
                        {
                            case (byte)SQPDynamicType.Byte:
                                ser.WriteByte(players[i].fields[j].valueByte);
                                break;
                            case (byte)SQPDynamicType.Ushort:
                                ser.WriteUShortSQP(players[i].fields[j].valueUShort);
                                break;
                            case (byte)SQPDynamicType.Uint:
                                ser.WriteUIntSQP(players[i].fields[j].valueUInt);
                                break;
                            case (byte)SQPDynamicType.Ulong:
                                ser.WriteULongSQP(players[i].fields[j].valueULong);
                                break;
                            case (byte)SQPDynamicType.String:
                                ser.WriteStringSQP(players[i].fields[j].valueString);
                                break;
                            default:
                                Debug.Log("incorrect field type specified");
                                break;
                        }
                    }
                }
            }

            return 0;
        }
    }

    public class SQPTeamResponsePacket
    {
        public ushort teamCount;
        public byte fieldCount;
        public List<SQPFieldContainer> teams;

        public SQPTeamResponsePacket() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteUShortSQP(teamCount);
            ser.WriteByte(fieldCount);

            // Serialize all the fields key/type
            if(teams.Count > 0)
            {
                for (int i = 0; i < teams[0].fields.Count; i++)
                {
                    ser.WriteStringSQP(teams[0].fields[i].key);
                    ser.WriteByte(teams[0].fields[i].type);
                }

                for(int i = 0; i < teams.Count; i++)
                {
                    for(int j = 0; j < teams[i].fields.Count; j++)
                    {
                        switch (teams[i].fields[j].type)
                        {
                            case (byte)SQPDynamicType.Byte:
                                ser.WriteByte(teams[i].fields[j].valueByte);
                                break;
                            case (byte)SQPDynamicType.Ushort:
                                ser.WriteUShortSQP(teams[i].fields[j].valueUShort);
                                break;
                            case (byte)SQPDynamicType.Uint:
                                ser.WriteUIntSQP(teams[i].fields[j].valueUInt);
                                break;
                            case (byte)SQPDynamicType.Ulong:
                                ser.WriteULongSQP(teams[i].fields[j].valueULong);
                                break;
                            case (byte)SQPDynamicType.String:
                                ser.WriteStringSQP(teams[i].fields[j].valueString);
                                break;
                            default:
                                Debug.Log("incorrect field type specified");
                                break;
                        }
                    }
                }
            }

            return 0;
        }
    }

    public class SQPQueryResponsePacket
    {
        public byte requestedChunks;
        public SQPPacketHeader header;
        public ushort version;
        public byte CurrentPacket;
        public byte lastPacket;
        public ushort packetLength;
        public uint serverInfoChunkLength;
        public SQPInfoResponsePacket serverInfoData;
        public uint rulesChunkLength;
        public SQPRulesResponsePacket serverRulesData;
        public uint playerInfoChunkLength;
        public SQPPlayerResponsePacket playerInfodata;
        public uint teamInfoChunkLength;
        public SQPTeamResponsePacket teamInfoData;

        public SQPQueryResponsePacket() 
        {
            serverInfoData = new SQPInfoResponsePacket();
            serverRulesData = new SQPRulesResponsePacket();
            playerInfodata = new SQPPlayerResponsePacket();
            teamInfoData = new SQPTeamResponsePacket();
        }

        public int Serialize(Serializer ser)
        {
            long serverInfoChunkStartPos = 0;
            long rulesChunkStartPos = 0;
            long playerInfoChunkStartPos = 0;
            long teamInfoChunkStartPos = 0;
            bool requestedServerInfo = (requestedChunks & (byte)SQPChunkType.ServerInfo) > 0;
            bool requestedServerRules = (requestedChunks & (byte)SQPChunkType.ServerRules) > 0;
            bool requestedPlayerInfo = (requestedChunks & (byte)SQPChunkType.PlayerInfo) > 0;
            bool requestedTeamInfo = (requestedChunks & (byte)SQPChunkType.TeamInfo) > 0;

            header.Serialize(ser);
            ser.WriteUShortSQP(version);
            ser.WriteByte(CurrentPacket);
            ser.WriteByte(lastPacket);
            ser.WriteUShortSQP(packetLength);

            if (requestedServerInfo)
            {
                // Write a placeholder chunkLength. This will be overwritten later.
                ser.WriteUIntSQP(serverInfoChunkLength);

                // Place a marker where the Chunk is started
                serverInfoChunkStartPos = ser.Size;

                serverInfoData.Serialize(ser);

                // Determine how long the Chunk is
                serverInfoChunkLength = (uint)(ser.Size - serverInfoChunkStartPos);
            }

            if (requestedServerRules)
            {
                // Write a placeholder chunkLength. This will be overwritten later.
                ser.WriteUIntSQP(rulesChunkLength);

                // Place a marker where the Chunk is started
                rulesChunkStartPos = ser.Size;

                serverRulesData.Serialize(ser);

                // Determine how long the Chunk is
                rulesChunkLength = (uint)(ser.Size - rulesChunkStartPos);
            }

            if (requestedPlayerInfo)
            {
                // Write a placeholder chunkLength. This will be overwritten later.
                ser.WriteUIntSQP(playerInfoChunkLength);

                // Place a marker where the Chunk is started
                playerInfoChunkStartPos = ser.Size;

                playerInfodata.Serialize(ser);

                // Determine how long the Chunk is
                playerInfoChunkLength = (uint)(ser.Size - playerInfoChunkStartPos);
            }

            if (requestedTeamInfo)
            {
                // Write a placeholder chunkLength. This will be overwritten later.
                ser.WriteUIntSQP(teamInfoChunkLength);

                // Place a marker where the Chunk is started
                teamInfoChunkStartPos = ser.Size;

                teamInfoData.Serialize(ser);

                // Determine how long the Chunk is
                teamInfoChunkLength = (uint)(ser.Size - teamInfoChunkStartPos);
            }

            if (requestedServerInfo)
            {
                ser.MoveCursor((int)(serverInfoChunkStartPos) - Serializer.UIntSize);
                ser.WriteUIntSQP(serverInfoChunkLength);
            }

            if (requestedServerRules)
            {
                ser.MoveCursor((int)(rulesChunkStartPos) - Serializer.UIntSize);
                ser.WriteUIntSQP(rulesChunkLength);
            }

            if (requestedPlayerInfo)
            {
                ser.MoveCursor((int)(playerInfoChunkStartPos) - Serializer.UIntSize);
                ser.WriteUIntSQP(playerInfoChunkLength);
            }

            if (requestedTeamInfo)
            {
                ser.MoveCursor((int)(teamInfoChunkStartPos) - Serializer.UIntSize);
                ser.WriteUIntSQP(teamInfoChunkLength);
            }

            return 0;
        }
    }
}
