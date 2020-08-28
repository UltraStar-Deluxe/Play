using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CharacterQuickJumpBar : MonoBehaviour, INeedInjection
{
    // Quick jump letters that are also found on an Android contacts list.
    // Q, X, Y and Z are skipped (probably because nearly never used).
    private static readonly string characters = "&ABCDEFGHIJKLMNOPRSTUVW#";

    public CharacterQuickJump characterQuickJumpPrefab;

    [Inject]
    private Injector injector;

    [Inject]
    private OrderSlider orderSlider;

    [Inject]
    private SongSelectSceneController songSelectSceneController;

    [Inject]
    private SongMetaManager songMetaManager;

    private bool isSongMetasOutdated;

    void Start()
    {
        UpdateCharacters();
        orderSlider.Selection.Subscribe(newSongOrder => UpdateCharacters());
        songMetaManager.SongScanFinishedEventStream.Subscribe(_ => isSongMetasOutdated = true);
    }

    void Update()
    {
        if (isSongMetasOutdated)
        {
            Debug.Log("Song Metas Outdated");
            isSongMetasOutdated = false;
            songSelectSceneController.GetSongMetasFromManager();
            songSelectSceneController.UpdateFilteredSongs();
            UpdateCharacters();
        }
    }

    private void UpdateCharacters()
    {
        GetComponentsInChildren<CharacterQuickJump>().ForEach(it => Destroy(it.gameObject));

        foreach (char c in characters.ToLowerInvariant())
        {
            CreateCharacter(c);
        }
    }

    private void CreateCharacter(char character)
    {
        CharacterQuickJump characterQuickJump = Instantiate(characterQuickJumpPrefab, transform);
        characterQuickJump.character = character.ToString();
        injector.Inject(characterQuickJump);

        SongMeta match = songSelectSceneController.GetCharacterQuickJumpSongMeta(character);
        if (match == null)
        {
            characterQuickJump.Interactable = false;
        }
    }
}
