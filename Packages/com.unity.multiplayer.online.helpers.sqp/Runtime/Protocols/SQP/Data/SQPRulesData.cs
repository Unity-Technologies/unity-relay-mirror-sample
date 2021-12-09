using System.Collections.Generic;
using Unity.Helpers.ServerQuery.Protocols.SQP.Collections;

namespace Unity.Helpers.ServerQuery.Protocols.SQP.Data
{
    public class SQPRulesData
    {
        public List<SQPServerRule> rules { get; set; }

        public SQPRulesData()
        {
            // Default rule
            rules = new List<SQPServerRule>();
        }
    }
}