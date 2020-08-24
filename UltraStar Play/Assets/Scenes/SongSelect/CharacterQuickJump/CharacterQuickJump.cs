using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UniRx.Triggers;
using UnityEngine.EventSystems;
using System;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CharacterQuickJump : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongSelectSceneController songSelectSceneController;

    [Inject]
    private SongRouletteController songRouletteController;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Button uiButton;

    public string character;

    public bool Interactable
    {
        get
        {
            return uiButton.interactable;
        }
        set
        {
            uiButton.interactable = value;
        }
    }

    void Start()
    {
        uiText.text = character.ToUpper();

        uiButton.OnClickAsObservable()
            .Subscribe(_ => DoCharacterQuickJump());
        this.ObserveEveryValueChanged(me => me.character).WhereNotNull()
            .Subscribe(newCharacter => uiText.text = newCharacter.ToUpper());
    }

    private void DoCharacterQuickJump()
    {
        if (!character.IsNullOrEmpty())
        {
            SongMeta match = songSelectSceneController.GetCharacterQuickJumpSongMeta(character.ToLower()[0]);
            if (match != null)
            {
                songRouletteController.SelectSong(match);
            }
        }
    }
}
