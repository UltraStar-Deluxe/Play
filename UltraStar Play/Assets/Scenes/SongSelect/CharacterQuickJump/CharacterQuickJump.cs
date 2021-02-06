using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CharacterQuickJump : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongSelectSceneController songSelectSceneController;

    [Inject]
    private SongRouletteController songRouletteController;

    [Inject]
    private EventSystem eventSystem;
    
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    public Button UiButton { get; private set; }

    public string character;

    private List<IDisposable> disposables = new List<IDisposable>();
    
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

        disposables.Add(InputManager.GetInputAction(R.InputActions.ui_submit).PerformedAsObservable()
            .Where(_ => eventSystem.currentSelectedGameObject == UiButton.gameObject)
            .Subscribe(_ => DoCharacterQuickJump()));
    }

    private void DoCharacterQuickJump()
    {
        if (!character.IsNullOrEmpty())
        {
            SongMeta match = songSelectSceneController.GetCharacterQuickJumpSongMeta(character.ToLowerInvariant()[0]);
            if (match != null)
            {
                songRouletteController.SelectSong(match);
            }
        }
    }

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
    }
}
