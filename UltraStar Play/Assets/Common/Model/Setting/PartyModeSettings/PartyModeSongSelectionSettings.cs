using System;

[Serializable]
public class PartyModeSongSelectionSettings
{
    public EPartyModeSongSelectionMode songSelectionMode = EPartyModeSongSelectionMode.Manual;
    public IPlaylist songPoolPlaylist = UltraStarAllSongsPlaylist.Instance;
    public int jokerCount = 5;
}
