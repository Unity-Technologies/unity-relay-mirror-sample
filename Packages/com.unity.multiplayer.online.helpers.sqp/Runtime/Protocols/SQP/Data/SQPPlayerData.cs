using System.Collections.Generic;
using Unity.Helpers.ServerQuery.Protocols.SQP.Collections;

namespace Unity.Helpers.ServerQuery.Protocols.SQP.Data
{
    public class SQPPlayerData
    {
        public ushort playerCount { get; set; }
        public byte fieldCount { get; set; }
        public List<SQPFieldContainer> players { get; set; }

        public SQPPlayerData()
        {
            // Default values
            playerCount = 0;
            fieldCount = 0;

            players = new List<SQPFieldContainer>();
        }
    }
}