using UniRx;
using UnityEngine.UIElements;

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
        ImageManager.LoadSpriteFromUri($"{modFolder}/images/coins/Gold_1.png")
            .Subscribe(sprite => visualElement.style.backgroundImage = new StyleBackground(sprite));

        targetNoteControl.VisualElement.Add(visualElement);
    }
}
