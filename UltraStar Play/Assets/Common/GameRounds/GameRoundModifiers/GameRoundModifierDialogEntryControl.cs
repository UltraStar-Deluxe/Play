using System.Collections.Generic;
using UnityEngine.UIElements;

public class GameRoundModifierDialogEntryControl
{
    public VisualElement VisualElement { get; private set; }
    public GameRoundSettings GameRoundSettings { get; private set; }

    private SlideToggle toggle;

    public GameRoundModifierDialogEntryControl(IGameRoundModifier modifier, GameRoundSettings gameRoundSettings)
    {
        this.GameRoundSettings = gameRoundSettings;

        VisualElement = new();
        VisualElement.name = $"{modifier.GetId()}Container";
        VisualElement.AddToClassList("gameRoundModifierConfigurationRoot");

        VisualElement modifierConfigVisualElement = modifier.CreateConfigurationVisualElement();
        if (modifierConfigVisualElement != null)
        {
            modifierConfigVisualElement.name = $"{modifier.GetId()}Configuration";
            modifierConfigVisualElement.AddToClassList("gameRoundModifierConfiguration");
            modifierConfigVisualElement.AddToClassList("child-mb-1");
            modifierConfigVisualElement.SetVisibleByDisplay(GameRoundSettings.modifiers.Contains(modifier));
        }

        toggle = new();
        toggle.name = $"{modifier.GetId()}Toggle";
        toggle.AddToClassList("gameRoundModifierToggle");
        toggle.SetTranslatedLabel(Translation.Of(modifier.DisplayName));
        FieldBindingUtils.Bind(toggle,
            () => GameRoundSettings.modifiers.Contains(modifier),
            newValue =>
            {
                if (newValue)
                {
                    GameRoundSettings.modifiers.Add(modifier);
                }
                else
                {
                    GameRoundSettings.modifiers.Remove(modifier);
                }
                modifierConfigVisualElement?.SetVisibleByDisplay(newValue);
            });

        VisualElement.Add(toggle);
        if (modifierConfigVisualElement != null)
        {
            VisualElement.Add(modifierConfigVisualElement);
        }
    }

    public void Reset()
    {
        toggle.value = false;
    }

    public void Randomize()
    {
        toggle.value = RandomUtils.RandomOf(new List<bool> { true,false });
    }
}

