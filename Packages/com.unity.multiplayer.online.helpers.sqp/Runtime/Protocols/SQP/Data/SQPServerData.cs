namespace Unity.Helpers.ServerQuery.Protocols.SQP.Data
{
    public class SQPServerData
    {
        public int currentPlayers { get; set; }

        public int maxPlayers { get; set; }

        public string serverName { get; set; }

        public string gameType { get; set; }

        public string buildID { get; set; }

        public string map { get; set; }

        public ushort gamePort { get; set; }

        public SQPServerData()
        {
            currentPlayers = 0;
            maxPlayers = int.MaxValue;
            serverName = "";
            gameType = "";
            buildID = "";
            map = "";
            gamePort = 0;
        }
    }
}