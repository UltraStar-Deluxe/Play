using System;

[Serializable]
public class PartyModeRoundSettingsPreset
{
    public string name;
    public GameRoundSettings gameRoundSettings;

    public PartyModeRoundSettingsPreset(string name, GameRoundSettings gameRoundSettings)
    {
        this.name = name;
        this.gameRoundSettings = gameRoundSettings;
    }
}
