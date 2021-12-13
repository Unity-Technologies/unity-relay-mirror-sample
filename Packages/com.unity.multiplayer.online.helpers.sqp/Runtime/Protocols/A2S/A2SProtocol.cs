using System.Net;
using UnityEngine;
using Unity.Helpers.ServerQuery.Data;
using Unity.Helpers.ServerQuery.Protocols.A2S.Collections;

namespace Unity.Helpers.ServerQuery.Protocols.A2S
{
    public class A2SProtocol : IProtocol
    {
        public const int kA2SChallengeNumber = 2;
        public byte[] ReceiveData(byte[] data, IPEndPoint remoteClient, uint serverID)
        {
            // Don't proceed if we've received a packet thats too small
            if (data.Length < 9) return null;

            // Parse packet header
            Serializer ser = new Serializer(data, Serializer.SerializationMode.Read);
            var requestPacket = A2SRequestPacket.Deserialize(ser);

            // The packet is a challenge request
            switch (requestPacket.requestType)
            {
                // The packet is a challenge request
                case ((byte)'T'):
                    Debug.Log("sending A2S info packet");
                    return SendA2SInfoPacket(serverID);
                // The packet is a query request
                case ((byte)'V'):
                    if(requestPacket.challengeNumber == -1)
                    {
                        return IssueA2SChallengeNumber();
                    }
                    else if (requestPacket.challengeNumber == kA2SChallengeNumber)
                    {
                        return SendA2SRulesPacket(serverID);
                    }
                    else
                    {
                        Debug.Log("Received invalid challenge number");
                        return null;
                    }
                case ((byte)'U'):
                    if (requestPacket.challengeNumber == -1)
                    {
                        return IssueA2SChallengeNumber();
                    }
                    else if (requestPacket.challengeNumber == kA2SChallengeNumber)
                    {
                        return SendA2SPlayerPacket(serverID);
                    }
                    else
                    {
                        Debug.Log("Received invalid challenge number");
                        return null;
                    }
                default:
                    Debug.Log("Unsupported/Unimplemented A2S request type received");
                    return null;
            }
        }

        private byte[] SendA2SInfoPacket(uint serverID)
        {
            var info = QueryDataProvider.GetServerData(serverID).A2SServerInfo;

            int packetSize =
                Serializer.IntSize + // Default header
                Serializer.ByteSize + // Response header
                Serializer.ByteSize + // Protocol
                Serializer.StringSize(info.serverName) + // Server name
                Serializer.StringSize(info.serverMap) + // Server map
                Serializer.StringSize(info.folder) + // Folder
                Serializer.StringSize(info.gameName) + // Game name
                Serializer.ShortSize + // Steam app id
                Serializer.ByteSize + // Player count
                Serializer.ByteSize + // Max players
                Serializer.ByteSize + // Bot count
                Serializer.ByteSize + // Server type
                Serializer.ByteSize + // Environment
                Serializer.ByteSize + // Visibility
                Serializer.ByteSize + // Valve anti-cheat
                Serializer.StringSize(info.version) + // Version
                Serializer.ByteSize + 1; // Extra data flag 

            byte[] response = new byte[packetSize];

            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);

            A2SInfoResponsePacket infoResponsePacket = new A2SInfoResponsePacket(); 
            infoResponsePacket.defaultHeader = -1;
            infoResponsePacket.responseHeader = (byte)'I';
            infoResponsePacket.protocol = info.protocol;
            infoResponsePacket.serverName = info.serverName;
            infoResponsePacket.serverMap = info.serverMap;
            infoResponsePacket.folder = info.folder;
            infoResponsePacket.gameName = info.gameName;
            infoResponsePacket.steamId = info.steamID;
            infoResponsePacket.playerCount = info.playerCount;
            infoResponsePacket.maxPlayers = info.maxPlayers;
            infoResponsePacket.botCount = info.botCount;
            infoResponsePacket.serverType = info.serverType;

            if(Application.platform == RuntimePlatform.WindowsPlayer)
            {
                infoResponsePacket.environment = (byte)'w';
            }
            else if (Application.platform == RuntimePlatform.LinuxPlayer)
            {
                infoResponsePacket.environment = (byte)'l';
            }
            else if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                infoResponsePacket.environment = (byte)'m';
            }

            infoResponsePacket.visibility = info.visibility; // All servers on multiplay are considered public
            infoResponsePacket.valveAntiCheat = info.valveAntiCheat;
            infoResponsePacket.version = info.version;
            infoResponsePacket.extraDataFlag = info.extraDataFlag;

            infoResponsePacket.Serialize(ser);

            return response;
        }
        private byte[] SendA2SPlayerPacket(uint serverID)
        {
            var info = QueryDataProvider.GetServerData(serverID).A2SPlayerInfo;
            int packetSize =
                Serializer.IntSize + // Default header
                Serializer.ByteSize + // Response header
                Serializer.ShortSize + 1; // Num players

            foreach(A2SPlayerResponsePacketPlayer player in info.players)
            {
                packetSize += Serializer.ByteSize + // Player index
                    Serializer.StringSize(player.playerName) + // Player name
                    Serializer.IntSize + // Player score
                    Serializer.FloatSize; // player duration
            }

            byte[] response = new byte[packetSize];

            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);

            A2SPlayerResponsePacket playerResponsePacket = new A2SPlayerResponsePacket();
            playerResponsePacket.defaultHeader = -1;
            playerResponsePacket.responseHeader = (byte)'D';
            playerResponsePacket.numPlayers = info.numPlayers;
            playerResponsePacket.players = info.players;

            playerResponsePacket.Serialize(ser);

            return response;
        }

        private byte[] SendA2SRulesPacket(uint serverID)
        {
            var info = QueryDataProvider.GetServerData(serverID).A2SServerRules;
            int packetSize =
                Serializer.IntSize + // Default header
                Serializer.ByteSize + // Response header
                Serializer.ShortSize + 1; // Num rules

            foreach(A2SRulesResponsePacketKeyValue rule in info.rules)
            {
                packetSize += Serializer.StringSize(rule.ruleName) + // Rule name
                    Serializer.StringSize(rule.ruleValue); // Rule value
            }

            byte[] response = new byte[packetSize];

            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);

            A2SRulesResponsePacket rulesResponsePacket = new A2SRulesResponsePacket();
            rulesResponsePacket.defaultHeader = -1;
            rulesResponsePacket.responseHeader = (byte)'E';
            rulesResponsePacket.numRules = info.numRules;
            rulesResponsePacket.rules = info.rules;

            rulesResponsePacket.Serialize(ser);

            return response;
        }

        private byte[] IssueA2SChallengeNumber()
        {
            Debug.Log("issuing A2S challenge number");

            int packetSize =
                Serializer.IntSize + // default header
                Serializer.ByteSize + // response header
                Serializer.IntSize + 1; // challenge number

            byte[] response = new byte[packetSize];

            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);

            A2SChallengeResponsePacket challengeResponsePacket = new A2SChallengeResponsePacket();
            challengeResponsePacket.defaultHeader = -1;
            challengeResponsePacket.responseHeader = (byte)'A';
            challengeResponsePacket.challengeNumber = kA2SChallengeNumber;

            challengeResponsePacket.Serialize(ser);

            return response;
        }

        public int GetDefaultPort()
        {
            return 7779;
        }
    }
}