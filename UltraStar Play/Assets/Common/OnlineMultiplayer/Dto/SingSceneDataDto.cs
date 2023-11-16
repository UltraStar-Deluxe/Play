using System.Collections.Generic;

namespace CommonOnlineMultiplayer
{
    public class SingSceneDataDto
    {
        public List<string> GloballyUniqueSongMetaIds { get; set; }
        public SingScenePlayerDataDto SingScenePlayerData { get; set; } = new();

        public int MedleySongIndex { get; set; } = -1;
        public bool IsMedley => MedleySongIndex >= 0;
        public GameRoundSettingsDto GameRoundSettings { get; set; } = new();
        public PartyModeSceneDataDto PartyModeSceneData { get; set; }

        public double PositionInSongInMillis { get; set; }
        public bool IsRestart { get; set; }
        public Dictionary<LobbyMember, List<PlayerScoreControlData>> LobbyMemberToScoreDataMap { get; set; } = new();
    }
}
