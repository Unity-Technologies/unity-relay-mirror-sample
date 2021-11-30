﻿namespace Unity.Helpers.ServerQuery.Protocols.A2S.Collections
{
    public class A2SRequestPacket
    {
        public int defaultHeader;
        public byte requestType;
        public int challengeNumber;

        public A2SRequestPacket() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteInt(-1);
            ser.WriteByte(requestType);
            ser.WriteInt(challengeNumber);
            return 9;
        }

        public static A2SRequestPacket Deserialize(Serializer ser)
        {
            var RequestPacket = new A2SRequestPacket
            {
                defaultHeader = ser.ReadInt(),
                requestType = ser.ReadByte(),
                challengeNumber = ser.ReadInt()
            };
            return RequestPacket;
        }
    }

    public class A2SInfoResponsePacket
    {
        public int defaultHeader;
        public byte responseHeader;
        public byte protocol;
        public string serverName;
        public string serverMap;
        public string folder;
        public string gameName;
        public short steamId;
        public byte playerCount;
        public byte maxPlayers;
        public byte botCount;
        public byte serverType; //d = dedicated, i = non-dedicated, p = sourceTV relay 
        public byte environment; // l = linux, w = windows, m = mac
        public byte visibility; //0 = public, 1 = private
        public byte valveAntiCheat; // 0 = no vac, 1 = vac
        public string version; // Version of the game installed on server
        public byte extraDataFlag;

        public A2SInfoResponsePacket() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteInt(-1);
            ser.WriteByte(responseHeader);
            ser.WriteByte(protocol);
            ser.WriteA2SString(serverName);
            ser.WriteA2SString(serverName);
            ser.WriteA2SString(folder);
            ser.WriteA2SString(gameName);
            ser.WriteShort(steamId); //how to handle, only ushort versions no short version
            ser.WriteByte(playerCount);
            ser.WriteByte(maxPlayers);
            ser.WriteByte(botCount);
            ser.WriteByte(serverType);
            ser.WriteByte(environment);
            ser.WriteByte(visibility);
            ser.WriteByte(valveAntiCheat);
            ser.WriteA2SString(version);
            ser.WriteByte(extraDataFlag);
            return 0;
        }

        public static A2SInfoResponsePacket Deserialize(Serializer ser)
        {
            return null;
        }
    }

    public class A2SPlayerResponsePacketPlayer
    {
        public byte index;
        public string playerName;
        public int score;
        public float duration;

        public A2SPlayerResponsePacketPlayer() { }
    }

    public class A2SPlayerResponsePacket
    {
        public int defaultHeader;
        public byte responseHeader;
        public byte numPlayers;
        public A2SPlayerResponsePacketPlayer[] players;

        public A2SPlayerResponsePacket() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteInt(-1);
            ser.WriteByte(responseHeader);
            ser.WriteByte(numPlayers);
            for (int i = 0; i < players.Length; i++)
            {
                ser.WriteByte(players[i].index);
                ser.WriteA2SString(players[i].playerName);
                ser.WriteInt(players[i].score);
                ser.WriteFloat(players[i].duration); // TODO: write float?
            }
            return 0;
        }

        public static A2SRulesResponsePacket Deserialize(Serializer ser)
        {
            return null;
        }
    }

    public class A2SRulesResponsePacketKeyValue
    {
        public string ruleName;
        public string ruleValue;

        public A2SRulesResponsePacketKeyValue() { }
    }

    public class A2SRulesResponsePacket
    {
        public int defaultHeader;
        public byte responseHeader;
        public short numRules;
        public A2SRulesResponsePacketKeyValue[] rules;

        public A2SRulesResponsePacket() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteInt(-1);
            ser.WriteByte(responseHeader);
            ser.WriteShort(numRules);
            for (int i = 0; i < rules.Length; i++)
            {
                ser.WriteA2SString(rules[i].ruleName);
                ser.WriteA2SString(rules[i].ruleValue);
            }
            return 0;
        }

        public static A2SRulesResponsePacket Deserialize(Serializer ser)
        {
            return null;
        }
    }

    public class A2SChallengeResponsePacket
    {
        public int defaultHeader;
        public byte responseHeader;
        public int challengeNumber;

        public A2SChallengeResponsePacket() { }

        public int Serialize(Serializer ser)
        {
            ser.WriteInt(-1);
            ser.WriteByte(responseHeader);
            ser.WriteInt(challengeNumber);
            return 9;
        }

        public static A2SChallengeResponsePacket Deserialize(Serializer ser)
        {
            var RequestPacket = new A2SChallengeResponsePacket
            {
                defaultHeader = ser.ReadInt(),
                responseHeader = ser.ReadByte(),
                challengeNumber = ser.ReadInt()
            };
            return RequestPacket;
        }
    }
}
