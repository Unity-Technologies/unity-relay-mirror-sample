namespace Unity.Helpers.ServerQuery.Data
{
    public class QueryData
    {
        public ServerData ServerInfo { get; set; }

        public RulesData ServerRules { get; set; }

        public PlayerData PlayerInfo { get; set; }

        public TeamData TeamInfo { get; set; }

        public QueryData()
        {
            ServerInfo = new ServerData();
            ServerRules = new RulesData();
            PlayerInfo = new PlayerData();
            TeamInfo = new TeamData();
        }
    }
}