using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CharacterQuickJumpCharacterControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.characterQuickJumpCharacter)]
    private Label label;

    [Inject(UxmlName = R.UxmlNames.characterQuickJumpCharacterButton)]
    private Button characterButton;

    [Inject]
    private CharacterQuickJumpListControl characterQuickJumpListControl;

    [Inject]
    private GameObject gameObject;

    public char Character { get; private set; }

    public VisualElement VisualElement { get; private set; }

    public bool Enabled
    {
        get
        {
            return characterButton.enabledSelf;
        }
        set
        {
            characterButton.SetEnabled(value);
            VisualElement.SetVisibleByDisplay(value);
        }
    }

    public CharacterQuickJumpCharacterControl(VisualElement visualElement, char character)
    {
        this.VisualElement = visualElement;
        this.Character = character;
    }

    public void OnInjectionFinished()
    {
        label.text = Character.ToString().ToUpperInvariant();

        characterButton.RegisterCallbackButtonTriggered(() => characterQuickJumpListControl.DoCharacterQuickJump(Character));
        this.ObserveEveryValueChanged(me => me.Character)
            .WhereNotNull()
            .Subscribe(newCharacter => label.text = newCharacter.ToString().ToUpperInvariant())
            .AddTo(gameObject);
    }
}
