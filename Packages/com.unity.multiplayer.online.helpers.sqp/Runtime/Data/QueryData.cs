using Unity.Helpers.ServerQuery.Protocols.SQP.Data;
using Unity.Helpers.ServerQuery.Protocols.A2S.Data;
using Unity.Helpers.ServerQuery.Protocols.TF2E.Data;

namespace Unity.Helpers.ServerQuery.Data
{
    public class QueryData
    {
        public SQPServerData SQPServerInfo { get; set; }

        public SQPRulesData SQPServerRules { get; set; }

        public SQPPlayerData SQPPlayerInfo { get; set; }

        public SQPTeamData SQPTeamInfo { get; set; }

        public A2SServerData A2SServerInfo { get; set; }

        public A2SRulesData A2SServerRules { get; set; }

        public A2SPlayerData A2SPlayerInfo { get; set; }

        public TF2EQueryData TF2EQueryInfo { get; set; }

        public QueryData()
        {
            SQPServerInfo = new SQPServerData();
            SQPServerRules = new SQPRulesData();
            SQPPlayerInfo = new SQPPlayerData();
            SQPTeamInfo = new SQPTeamData();
            A2SServerInfo = new A2SServerData();
            A2SServerRules = new A2SRulesData();
            A2SPlayerInfo = new A2SPlayerData();
            TF2EQueryInfo = new TF2EQueryData();
        }
    }
}