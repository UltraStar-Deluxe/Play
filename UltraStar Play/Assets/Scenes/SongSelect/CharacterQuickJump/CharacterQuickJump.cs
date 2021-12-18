using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using PrimeInputActions;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CharacterQuickJump : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongSelectSceneUiControl songSelectSceneUiControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private EventSystem eventSystem;
    
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public Button UiButton { get; private set; }

    public string character;

    public bool Interactable
    {
        get
        {
            return UiButton.interactable;
        }
        set
        {
            UiButton.interactable = value;
        }
    }

    void Start()
    {
        uiText.text = character.ToUpperInvariant();

        UiButton.OnClickAsObservable()
            .Subscribe(_ => DoCharacterQuickJump());
        this.ObserveEveryValueChanged(me => me.character).WhereNotNull()
            .Subscribe(newCharacter => uiText.text = newCharacter.ToUpperInvariant());

        InputManager.GetInputAction(R.InputActions.ui_submit).PerformedAsObservable()
            .Where(_ => eventSystem.currentSelectedGameObject == UiButton.gameObject)
            .Subscribe(_ => DoCharacterQuickJump())
            .AddTo(gameObject);
    }

    private void DoCharacterQuickJump()
    {
        if (!character.IsNullOrEmpty())
        {
            SongMeta match = songSelectSceneUiControl.GetCharacterQuickJumpSongMeta(character.ToLowerInvariant()[0]);
            if (match != null)
            {
                songRouletteControl.SelectSong(match);
            }
        }
    }
}
