using System.Collections.Generic;

public class GameRoundSettingsDto
{
    public List<GameRoundModifierDto> ModifierDtos { get; set; } = new();
    public bool AnyModifierActive => !ModifierDtos.IsNullOrEmpty();
}
