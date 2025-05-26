using UnityEngine.UIElements;
using UnityEngine;

public class CoinControl
{
    private readonly string modFolder;

    private readonly TargetNoteControl targetNoteControl;
    public TargetNoteControl TargetNoteControl => targetNoteControl;

    private readonly VisualElement visualElement;
    public VisualElement VisualElement => visualElement;

    public CoinControl(string modFolder, TargetNoteControl targetNoteControl)
    {
        this.modFolder = modFolder;
        this.targetNoteControl = targetNoteControl;

        this.visualElement = new VisualElement();
        visualElement.name = "coinCollectorCoin";
        targetNoteControl.VisualElement.Add(visualElement);

        LoadSprite();
    }

    private async void LoadSprite()
    {
        Sprite sprite = await ImageManager.LoadSpriteFromUriAsync($"{modFolder}/images/coins/Gold_1.png");
        visualElement.style.backgroundImage = new StyleBackground(sprite);
    }
}
