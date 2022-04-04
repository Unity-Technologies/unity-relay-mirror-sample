using System.Collections.Generic;

namespace Unity.Helpers.ServerQuery.Protocols.TF2E.Collections
{
    enum TF2EMessageType : byte
    {
        ServerInfoRequest = 77,
        ServerInfoResponse = 78
    }

    public enum TF2EHealthFlags : int
    {
        None = 0,

        PacketLossIn = 1 << 0,
        PacketLossOut = 1 << 1,
        PacketChokedIn = 1 << 2,
        PacketChokedOut = 1 << 3,
        SlowServerFrames = 1 << 4,
        Hitching = 1 << 5
    }

    public class TF2EInstanceInfo
    {
        public byte retail;
        public byte instanceType;
        public uint clientCRC;
        public ushort netProtocol;
        public uint healthFlags;
        public uint randomServerId;
        public TF2EInstanceInfo() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteByte(retail);
            ser.WriteByte(instanceType);
            ser.WriteUInt(clientCRC);
            ser.WriteUShort(netProtocol);
            ser.WriteUInt(healthFlags);
            ser.WriteUInt(randomServerId);

            return 0;
        }
        public static TF2EInstanceInfo Deserialize(Serializer ser)
        {
            return null;
        }
    }

    public class TF2EBasicInfo
    {
        public ushort port;
        public string platform;
        public string playlistVersion;
        public uint playlistNum;
        public string playlistName;

        public byte platformNum;
        public Dictionary<string, byte> platformPlayers;

        public byte numClients;
        public byte maxClients;
        public string map;

        public TF2EBasicInfo() 
        {
            platformPlayers = new Dictionary<string, byte>();
        }

        public int Serialize(Serializer ser)
        {
            ser.WriteUShort(port);
            ser.WriteString(platform);
            ser.WriteString(playlistVersion);
            ser.WriteUInt(playlistNum);
            ser.WriteString(playlistName);
            ser.WriteByte((byte)platformPlayers.Count);

            foreach (KeyValuePair<string, byte> entry in platformPlayers)
            {
                ser.WriteString(entry.Key);
                ser.WriteByte(entry.Value);
            }

            ser.WriteByte(numClients);
            ser.WriteByte(maxClients);
            ser.WriteString(map);

            return 0;
        }
    }

    public class TF2EPerformanceInfo
    {
        public float averageFrameTime;
        public float maxFrameTime;
        public float averageUserCommandTime;
        public float maxUserCommandTime;

        public TF2EPerformanceInfo() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteFloat(averageFrameTime);
            ser.WriteFloat(maxFrameTime);
            ser.WriteFloat(averageUserCommandTime);
            ser.WriteFloat(maxUserCommandTime);

            return 0;
        }
    }

    public class TF2EMatchState
    {
        public byte phase;
        public byte maxRounds;
        public byte roundsWonIMC;
        public byte roundsWonMilitia;
        public ushort timeLimitSeconds;
        public ushort timePassedSeconds;
        public ushort maxScore;
        public ushort teamsLeftWithPlayersNum;

        public TF2EMatchState() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteByte(phase);
            ser.WriteByte(maxRounds);
            ser.WriteByte(roundsWonIMC);
            ser.WriteByte(roundsWonMilitia);
            ser.WriteUShort(timeLimitSeconds);
            ser.WriteUShort(timePassedSeconds);
            ser.WriteUShort(maxScore);
            ser.WriteUShort(teamsLeftWithPlayersNum);

            return 0;
        }
    }

    public class TF2ETeam
    {
        public byte id;
        public ushort score;

        public TF2ETeam() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteByte(id);
            ser.WriteUShort(score);

            return 0;
        }
    }

    public class TF2EClient
    {
        public ulong id;
        public string name;
        public byte teamID;
        public string address;
        public uint ping;
        public uint packetsReceived;
        public uint packetsDropped;
        public uint score;
        public ushort kills;
        public ushort deaths;

        public TF2EClient() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteULong(id);
            ser.WriteString(name);
            ser.WriteByte(teamID);
            ser.WriteString(address);
            ser.WriteUInt(ping);
            ser.WriteUInt(packetsReceived);
            ser.WriteUInt(packetsDropped);
            ser.WriteUInt(score);
            ser.WriteUShort(kills);
            ser.WriteUShort(deaths);

            return 0;
        }
    }

    public class TF2ERequestPacket
    {
        public int defaultHeader;
        public byte requestType;
        public byte version;
        public TF2ERequestPacket() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteInt(-1);
            ser.WriteByte(requestType);
            ser.WriteByte(version);
            return 9;
        }
        public static TF2ERequestPacket Deserialize(Serializer ser)
        {
            var RequestPacket = new TF2ERequestPacket
            {
                defaultHeader = ser.ReadInt(),
                requestType = ser.ReadByte(),
                version = ser.ReadByte()
            };
            return RequestPacket;
        }
    }

    public class TF2EResponseHeader
    {
        public int prefix;
        public byte command;
        public byte version;

        public TF2EResponseHeader() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteInt(prefix);
            ser.WriteByte(command);
            ser.WriteByte(version);

            return 0;
        }
    }

    public class TF2EResponsePacket
    {
        public TF2EResponseHeader Header;
        public TF2EInstanceInfo instanceInfo;
        public string buildName;
        public string dataCenter;
        public string gameMode;
        public TF2EBasicInfo basicInfo;
        public TF2EPerformanceInfo performanceInfo;
        public TF2EMatchState matchState;
        public List<TF2ETeam> teams;
        public List<TF2EClient> clients;

        public TF2EResponsePacket() 
        {
            Header = new TF2EResponseHeader();
            instanceInfo = new TF2EInstanceInfo();
            basicInfo = new TF2EBasicInfo();
            performanceInfo = new TF2EPerformanceInfo();
            matchState = new TF2EMatchState();
            teams = new List<TF2ETeam>();
            clients = new List<TF2EClient>();
        }

        public int Serialize(Serializer ser)
        {
            Header.Serialize(ser);
            instanceInfo.Serialize(ser);
            ser.WriteString(buildName);
            ser.WriteString(dataCenter);
            ser.WriteString(gameMode);
            basicInfo.Serialize(ser);
            performanceInfo.Serialize(ser);
            matchState.Serialize(ser);

            foreach (TF2ETeam team in teams)
            {
                team.Serialize(ser);
            }

            byte teamsTerminator = 255;
            ser.WriteByte(teamsTerminator);

            foreach (TF2EClient client in clients)
            {
                client.Serialize(ser);
            }

            ulong clientTerminator = 0;
            ser.WriteULong(clientTerminator);

            return 0;
        }
    }
}
