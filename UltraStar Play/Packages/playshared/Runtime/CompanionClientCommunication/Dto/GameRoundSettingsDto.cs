using System.Collections.Generic;
using System.Linq;

public class GameRoundSettingsDto
{
    public List<GameRoundModifierDto> ModifierDtos { get; set; } = new();
    public bool AnyModifierActive => !ModifierDtos.IsNullOrEmpty();

    public void AddModifier(GameRoundModifierDto newModifier)
    {
        if (newModifier == null)
        {
            return;
        }

        RemoveModifierById(newModifier.Id);
        ModifierDtos.Add(newModifier);
    }

    public void RemoveModifierById(string id)
    {
        ModifierDtos = ModifierDtos
            .Where(modifier => modifier.Id != id)
            .ToList();
    }

    public bool ContainsModifierWithId(string id)
    {
        return ModifierDtos.AnyMatch(modifier => modifier.Id == id);
    }
}
