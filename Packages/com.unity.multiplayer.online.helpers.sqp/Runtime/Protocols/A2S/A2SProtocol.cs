using System.Net;
using UnityEngine;
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
                    return SendA2SInfoPacket();
                // The packet is a query request
                case ((byte)'V'):
                    if(requestPacket.challengeNumber == -1)
                    {
                        return IssueA2SChallengeNumber();
                    }
                    else if (requestPacket.challengeNumber == kA2SChallengeNumber)
                    {
                        return SendA2SRulesPacket();
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
                        return SendA2SPlayerPacket();
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

        private byte[] SendA2SInfoPacket()
        {
            string version = "001";
            string[] args = System.Environment.GetCommandLineArgs();

            for(int i = 0; i < args.Length; i++)
            {
                if(args[i] == "-version")
                {
                    version = args[i + 1];
                    break;
                }
            }

            int packetSize =
                Serializer.IntSize + // default header
                Serializer.ByteSize + // response header
                Serializer.ByteSize + // protocol
                Serializer.StringSize("Unity Dedicated Server") + // server name
                Serializer.StringSize("Generic map name here") + // server map
                Serializer.StringSize("Return a path here") + // folder
                Serializer.StringSize("Unity Mirror Sample") + // game name
                Serializer.ShortSize + // steam app id
                Serializer.ByteSize + // player count
                Serializer.ByteSize + // max players
                Serializer.ByteSize + // bot count
                Serializer.ByteSize + // server type
                Serializer.ByteSize + // environment
                Serializer.ByteSize + // visibility
                Serializer.ByteSize + // valve anti-cheat
                Serializer.StringSize(version) + // version
                Serializer.ByteSize + 1; // extra data flag // TODO: off by one error here? 


            byte[] response = new byte[packetSize];

            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);

            A2SInfoResponsePacket infoResponsePacket = new A2SInfoResponsePacket(); 
            infoResponsePacket.defaultHeader = -1;
            infoResponsePacket.responseHeader = (byte)'I';
            infoResponsePacket.protocol = 0;
            infoResponsePacket.serverName = "Unity Dedicated Server";
            infoResponsePacket.serverMap = "Generic map name here";
            infoResponsePacket.folder = "Return a path here";
            infoResponsePacket.gameName = "Unity Mirror Sample";
            infoResponsePacket.steamId = 12345;
            infoResponsePacket.playerCount = 2;
            infoResponsePacket.maxPlayers = 16;
            infoResponsePacket.botCount = 2;
            infoResponsePacket.serverType = (byte)'d';

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

            infoResponsePacket.visibility = 0; // All servers on multiplay are considered public
            infoResponsePacket.valveAntiCheat = 0; // We are not using valve anti-cheat
            infoResponsePacket.version = version;
            infoResponsePacket.extraDataFlag = 0;

            infoResponsePacket.Serialize(ser);

            return response;
        }
        private byte[] SendA2SPlayerPacket()
        {
            int packetSize =
                Serializer.IntSize + // default header
                Serializer.ByteSize + // response header
                Serializer.ShortSize + // num players
                Serializer.ByteSize + // 1st player index
                Serializer.StringSize("TestName") + // 1st player name
                Serializer.IntSize + // 1st player score
                Serializer.FloatSize + 1; // 1st player duration

            byte[] response = new byte[packetSize];

            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);

            A2SPlayerResponsePacket playerResponsePacket = new A2SPlayerResponsePacket();
            playerResponsePacket.defaultHeader = -1;
            playerResponsePacket.responseHeader = (byte)'D';
            playerResponsePacket.numPlayers = 1;

            A2SPlayerResponsePacketPlayer player = new A2SPlayerResponsePacketPlayer();
            player.index = 0;
            player.playerName = "TestName";
            player.score = 23;
            player.duration = 23.5f;

            A2SPlayerResponsePacketPlayer[] playerList = new A2SPlayerResponsePacketPlayer[1];
            playerList[0] = player;
            playerResponsePacket.players = playerList;

            playerResponsePacket.Serialize(ser);

            return response;
        }

        private byte[] SendA2SRulesPacket()
        {
            int packetSize =
                Serializer.IntSize + // default header
                Serializer.ByteSize + // response header
                Serializer.ShortSize + // num rules
                Serializer.StringSize("TestRule") + // 1st rule name
                Serializer.StringSize("TestValue") + 1; // 1st rule value


            byte[] response = new byte[packetSize];

            Serializer ser = new Serializer(response, Serializer.SerializationMode.Write);

            A2SRulesResponsePacket rulesResponsePacket = new A2SRulesResponsePacket();
            rulesResponsePacket.defaultHeader = -1;
            rulesResponsePacket.responseHeader = (byte)'E';
            rulesResponsePacket.numRules = 1;

            A2SRulesResponsePacketKeyValue keyValue = new A2SRulesResponsePacketKeyValue();
            keyValue.ruleName = "TestRule";
            keyValue.ruleValue = "TestValue";

            A2SRulesResponsePacketKeyValue[] rulesList = new A2SRulesResponsePacketKeyValue[1];
            rulesList[0] = keyValue;
            rulesResponsePacket.rules = rulesList;

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