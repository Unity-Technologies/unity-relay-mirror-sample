using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using Unity.Helpers.ServerQuery.Data;
using Unity.Helpers.ServerQuery;
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
            header.Serialize(ser);

            //SQP version
            ser.WriteUShort(SQPVersion);

            // Current and last packet
            ser.WriteByte(0);
            ser.WriteByte(0);
            
            //Packet length
            ser.WriteUShort((ushort)packetLength);
            //Chunk length
            ser.WriteUInt((ushort)chunkLength);
            
            //Current players in game
            ser.WriteUShort((ushort)info.CurrentPlayers);
            //Max player in game
            ser.WriteUShort((ushort)info.MaxPlayers);
            
            //Server name
            ser.WriteString(info.ServerName);
            
            //Game type name
            ser.WriteString(info.GameType);
            
            //Build ID
            ser.WriteString(info.BuildID);
            
            //Map name
            ser.WriteString(info.Map);
            
            //Port number
            ser.WriteUShort(info.GamePort);
            
            return response;
        }

        private byte[] BuildServerRuleResponse(PacketHeader header, uint serverID)
        {
            return null;
        }

        private byte[] BuildPlayerInfoResponse(PacketHeader header, uint serverID)
        {
            return null;
        }

        private byte[] BuildTeamInfoResponse(PacketHeader header, uint serverID)
        {
            return null;
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