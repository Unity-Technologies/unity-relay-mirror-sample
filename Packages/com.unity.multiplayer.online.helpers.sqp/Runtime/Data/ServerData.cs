namespace Unity.Helpers.ServerQuery.Data
{
    public class ServerData
    {
        public int CurrentPlayers { get; set; }

        public int MaxPlayers { get; set; }

        public string ServerName { get; set; }

        public string GameType { get; set; }

        public string BuildID { get; set; }

        public string Map { get; set; }

        public ushort GamePort { get; set; }

        public ushort Version { get; set; }

        public ServerData()
        {
            CurrentPlayers = 0;
            MaxPlayers = int.MaxValue;
            ServerName = "Default Server Name";
            GameType = "Default Game Type";
            BuildID = "Default Build ID";
            Map = "Default Map Name";
            GamePort = 0;
            Version = 000;
        }
    }
}