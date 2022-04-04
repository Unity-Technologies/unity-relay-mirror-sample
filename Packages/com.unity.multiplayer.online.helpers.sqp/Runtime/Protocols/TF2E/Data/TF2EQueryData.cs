using System.Collections.Generic;
using Unity.Helpers.ServerQuery.Protocols.TF2E.Collections;

namespace Unity.Helpers.ServerQuery.Protocols.TF2E.Data
{
    public class TF2EQueryData
    {
        public string buildName { get; set; }

        public string dataCenter { get; set; }

        public string gameMode { get; set; }

        public TF2EBasicInfo basicInfo { get; set; }

        public TF2EPerformanceInfo performanceInfo { get; set; }

        public TF2EMatchState matchState { get; set; }

        public List<TF2ETeam> teams { get; set; }

        public List<TF2EClient> clients { get; set; }

        public TF2EQueryData()
        {
            buildName = "";
            dataCenter = "";
            gameMode = "";

            basicInfo = new TF2EBasicInfo();
            basicInfo.port = 0;
            basicInfo.platform = "";
            basicInfo.playlistVersion = "";
            basicInfo.playlistNum = 0;
            basicInfo.playlistName = "";
            basicInfo.numClients = 0;
            basicInfo.maxClients = 0;
            basicInfo.map = "";

            performanceInfo = new TF2EPerformanceInfo();
            performanceInfo.averageFrameTime = 0f;
            performanceInfo.maxFrameTime = 0f;
            performanceInfo.averageUserCommandTime = 0f;
            performanceInfo.maxUserCommandTime = 0f;

            matchState = new TF2EMatchState();
            matchState.phase = 0;
            matchState.maxRounds = 0;
            matchState.roundsWonIMC = 0;
            matchState.roundsWonMilitia = 0;
            matchState.timeLimitSeconds = 0;
            matchState.timePassedSeconds = 0;
            matchState.maxScore = 0;
            matchState.teamsLeftWithPlayersNum = 0;

            teams = new List<TF2ETeam>();

            clients = new List<TF2EClient>();
        }
    }
}