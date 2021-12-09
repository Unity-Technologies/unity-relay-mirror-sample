using System.Collections.Generic;
using Unity.Helpers.ServerQuery.Protocols.SQP.Collections;

namespace Unity.Helpers.ServerQuery.Protocols.SQP.Data
{
    public class SQPTeamData
    {
        public ushort teamCount { get; set; }
        public byte fieldCount { get; set; }
        public List<SQPFieldContainer> teams { get; set; }

        public SQPTeamData()
        {
            // Default team values
            teamCount = 0;
            fieldCount = 0;

            teams = new List<SQPFieldContainer>();
        }
    }
}