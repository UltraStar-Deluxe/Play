using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CharacterQuickJumpBar : MonoBehaviour, INeedInjection
{
    private static readonly string characters = "&ABCDEFGHIJKLMNOPQRSTUVWXYZ#";

    public CharacterQuickJump characterQuickJumpPrefab;

    [Inject]
    private Injector injector;

    [Inject]
    private OrderSlider orderSlider;

    [Inject]
    private SongSelectSceneController songSelectSceneController;

    [Inject]
    private SongMetaManager songMetaManager;
    
    [Inject]
    private EventSystem eventSystem;

    private bool isSongMetasOutdated;

    public List<CharacterQuickJump> CharacterQuickJumpEntries => GetComponentsInChildren<CharacterQuickJump>().ToList();
    public List<CharacterQuickJump> InteractableCharacterQuickJumpEntries => CharacterQuickJumpEntries
        .Where(it => it.Interactable)
        .ToList();
    public CharacterQuickJump FocusedInteractableCharacterQuickJumpEntry => InteractableCharacterQuickJumpEntries
        .FirstOrDefault(it => eventSystem.currentSelectedGameObject == it.UiButton.gameObject);
    public int FocusedInteractableCharacterQuickJumpEntryIndex => InteractableCharacterQuickJumpEntries.IndexOf(FocusedInteractableCharacterQuickJumpEntry);
    
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
        CharacterQuickJumpEntries.ForEach(it => Destroy(it.gameObject));
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
    
    public bool TrySelectNextControl()
    {
        if ((eventSystem.currentSelectedGameObject == null
             || eventSystem.currentSelectedGameObject.GetComponentInParent<CharacterQuickJump>() == null)
            && InteractableCharacterQuickJumpEntries.Count > 0)
        {
            InteractableCharacterQuickJumpEntries.First().UiButton.Select();
            GetComponent<RectTransformSlideIntoViewport>().SlideIn();
            return true;
        }
            
        CharacterQuickJump nextEntry = InteractableCharacterQuickJumpEntries.GetElementAfter(FocusedInteractableCharacterQuickJumpEntry, false);
        if (nextEntry != null)
        {
            nextEntry.UiButton.Select();
            return true;
        }

        return false;
    }
    
    public bool TrySelectPreviousControl()
    {
        if ((eventSystem.currentSelectedGameObject == null
             || eventSystem.currentSelectedGameObject.GetComponentInParent<CharacterQuickJump>() == null)
            && InteractableCharacterQuickJumpEntries.Count > 0)
        {
            InteractableCharacterQuickJumpEntries.Last().UiButton.Select();
            GetComponent<RectTransformSlideIntoViewport>().SlideIn();
            return true;
        }
        
        CharacterQuickJump nextEntry = InteractableCharacterQuickJumpEntries.GetElementBefore(FocusedInteractableCharacterQuickJumpEntry, false);
        if (nextEntry != null)
        {
            nextEntry.UiButton.Select();
            return true;
        }
        
        return false;
    }
}
