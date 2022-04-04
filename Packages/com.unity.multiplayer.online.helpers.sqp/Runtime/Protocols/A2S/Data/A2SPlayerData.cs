using System.Collections.Generic;
using Unity.Helpers.ServerQuery.Protocols.A2S.Collections;

namespace Unity.Helpers.ServerQuery.Protocols.A2S.Data
{
    public class A2SPlayerData
    {
        public byte numPlayers { get; set; }

        public List<A2SPlayerResponsePacketPlayer> players { get; set; }

    public A2SPlayerData()
        {
            // Default values
            numPlayers = 0;

            players = new List<A2SPlayerResponsePacketPlayer>();
        }
    }
}