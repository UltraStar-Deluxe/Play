public class PartyModeRoundSettingsPreset
{
    public string Name { get; set; }
    public GameRoundSettings GameRoundSettings { get; set; }

    public PartyModeRoundSettingsPreset(string name, GameRoundSettings gameRoundSettings)
    {
        Name = name;
        GameRoundSettings = gameRoundSettings;
    }
}
