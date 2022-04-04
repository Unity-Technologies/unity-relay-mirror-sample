using System;
using Unity.Helpers.ServerQuery;

namespace Unity.ServerQuery.Protocols.SQP
{
    public enum QueryType : byte
    {
        ServerInfo = 1 << 0,
        ServerRules = 1 << 1,
        PlayerInfo = 1 << 2,
        TeamInfo = 1 << 3
    }
        
    public struct QueryHeader
    {
        public UInt16 Version;
        public QueryType RequestedChunks;

        public static int Size()
        {
            return 2 + 1;
        }
            
        public int Serialize(ref byte[] data)
        {
            return Size();
        }

        public static QueryHeader Deserialize(Serializer ser)
        {
            QueryHeader header = new QueryHeader()
            {
                Version = ser.ReadUShort(),
                RequestedChunks = (QueryType)ser.ReadByte()
            };
            return header;
        }
    }
}