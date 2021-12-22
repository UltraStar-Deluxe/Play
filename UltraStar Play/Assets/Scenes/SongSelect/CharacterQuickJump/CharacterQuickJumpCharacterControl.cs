using UnityEngine;
using UniInject;
using UniRx;
using System;
using System.Collections.Generic;
using PrimeInputActions;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CharacterQuickJumpCharacterControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongSelectSceneUiControl songSelectSceneUiControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject(UxmlName = R.UxmlNames.characterLabel)]
    private Label label;

    [Inject(UxmlName = R.UxmlNames.characterButton)]
    private Button characterButton;

    private readonly string character;

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

    public CharacterQuickJumpCharacterControl(VisualElement visualElement, string character)
    {
        this.VisualElement = visualElement;
        this.character = character;
    }

    public void OnInjectionFinished()
    {
        label.text = character.ToUpperInvariant();

        characterButton.RegisterCallbackButtonTriggered(() => DoCharacterQuickJump());
        this.ObserveEveryValueChanged(me => me.character)
            .WhereNotNull()
            .Subscribe(newCharacter => label.text = newCharacter.ToUpperInvariant());
    }

    private void DoCharacterQuickJump()
    {
        if (character.IsNullOrEmpty())
        {
            return;
        }

        SongMeta match = songSelectSceneUiControl.GetCharacterQuickJumpSongMeta(character.ToLowerInvariant()[0]);
        if (match != null)
        {
            songRouletteControl.SelectSong(match);
        }
    }
}
