using System.Collections.Generic;
using Unity.Helpers.ServerQuery.Protocols.A2S.Collections;

namespace Unity.Helpers.ServerQuery.Protocols.A2S.Data
{
    public class A2SRulesData
    {
        public short numRules { get; set; }

        public List<A2SRulesResponsePacketKeyValue> rules { get; set; }

        public A2SRulesData()
        {
            // Default rule
            numRules = 0;

            rules = new List<A2SRulesResponsePacketKeyValue>();
        }
    }
}