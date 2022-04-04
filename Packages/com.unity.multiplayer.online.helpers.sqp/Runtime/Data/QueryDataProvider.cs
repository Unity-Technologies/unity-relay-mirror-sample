using System.Collections.Generic;

namespace Unity.Helpers.ServerQuery.Data
{
    public class QueryDataProvider
    {
        private static Dictionary<uint, QueryData> s_QueryData = new Dictionary<uint, QueryData>();
        private static uint s_nextServerID = 0;
        
        public static uint RegisterServer()
        {
            var id = s_nextServerID++;
            s_QueryData.Add(id, new QueryData());
            return id;
        }

        public static uint RegisterServer(QueryData data)
        {
            var id = s_nextServerID++;
            s_QueryData.Add(id, data);
            return id;
        }

        public static QueryData GetServerData(uint serverID)
        {
            return s_QueryData[serverID];
        }

        public static void UpdateData(uint serverID, QueryData newData)
        {
            s_QueryData[serverID] = newData;
        }

    }
}