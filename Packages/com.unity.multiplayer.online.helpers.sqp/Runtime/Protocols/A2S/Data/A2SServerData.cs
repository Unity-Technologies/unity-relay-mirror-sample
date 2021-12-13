using System.Collections.Generic;
namespace Unity.Helpers.ServerQuery.Protocols.A2S.Data
{
    public class A2SServerData
    {
        public byte protocol { get; set; }

        public string serverName { get; set; }

        public string serverMap { get; set; }

        public string folder { get; set; }

        public string gameName { get; set; }

        public short steamID { get; set; }

        public byte playerCount { get; set; }

        public byte maxPlayers { get; set; }

        public byte botCount { get; set; }

        public byte serverType { get; set; }

        public byte visibility { get; set; }

        public byte valveAntiCheat { get; set; }

        public string version { get; set; }

        public byte extraDataFlag { get; set; }

        public A2SServerData()
        {
            protocol = 0;
            serverName = "";
            serverMap = "";
            folder = "";
            gameName = "";
            steamID = 0;
            playerCount = 0;
            maxPlayers = byte.MaxValue;
            botCount = 0;
            serverType = (byte)'d';
            visibility = 0;
            valveAntiCheat = 0;
            version = "001";
            extraDataFlag = 0;
        }
    }
}