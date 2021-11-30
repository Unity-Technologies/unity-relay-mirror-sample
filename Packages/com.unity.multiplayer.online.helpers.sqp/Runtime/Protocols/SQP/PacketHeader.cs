using System;

namespace Unity.Helpers.ServerQuery.Protocols.SQP
{
    public enum PacketType
    {
        Challenge,
        Query
    }
    
    public struct PacketHeader
    {
        public PacketType Type;
        public UInt32 ChallengeToken;

        public int Serialize(Serializer ser)
        {
            ser.WriteByte((Type == PacketType.Challenge) ? (byte)0 : (byte)1);
            ser.WriteUInt(ChallengeToken);
            return Size();
        }
            
        public static PacketHeader Deserialize(Serializer ser)
        {
            var header = new PacketHeader
            {
                Type = (ser.ReadByte() == 0) ? PacketType.Challenge : PacketType.Query,
                ChallengeToken = ser.ReadUInt()
            };
            return header;
        }

        public static int Size()
        {
            return 1 + 4;
        }
    }
}