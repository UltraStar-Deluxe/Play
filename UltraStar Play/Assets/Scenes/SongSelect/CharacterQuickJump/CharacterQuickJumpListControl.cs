using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CharacterQuickJumpListControl : MonoBehaviour, INeedInjection
{
    private static readonly string characters = "&ABCDEFGHIJKLMNOPQRSTUVWXYZ#";

    [InjectedInInspector]
    public VisualTreeAsset characterUi;

    [Inject]
    private Injector injector;

    [Inject]
    private SongSelectSceneUiControl songSelectSceneUiControl;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject(UxmlName = R.UxmlNames.characterContainer)]
    private ScrollView characterContainer;

    [Inject(UxmlName = R.UxmlNames.previousCharacterButton)]
    private Button previousCharacterButton;

    [Inject(UxmlName = R.UxmlNames.nextCharacterButton)]
    private Button nextCharacterButton;

    private bool isSongMetasOutdated;

    private List<CharacterQuickJumpCharacterControl> characterQuickJumpEntryControls = new List<CharacterQuickJumpCharacterControl>();

    private void Start()
    {
        characterContainer.Clear();
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => UpdateCharacters()));

        previousCharacterButton.RegisterCallbackButtonTriggered(() => characterContainer.horizontalScroller.ScrollPageUp());
        nextCharacterButton.RegisterCallbackButtonTriggered(() => characterContainer.horizontalScroller.ScrollPageDown());

        // Update outdated song metas on main thread
        songMetaManager.SongScanFinishedEventStream.Subscribe(_ => isSongMetasOutdated = true);
    }

    void Update()
    {
        if (isSongMetasOutdated)
        {
            Debug.Log("Song Metas outdated");
            isSongMetasOutdated = false;
            songSelectSceneUiControl.InitSongMetas();
            songSelectSceneUiControl.UpdateFilteredSongs();
            UpdateCharacters();
        }
    }

    public void UpdateCharacters()
    {
        characterQuickJumpEntryControls.ForEach(it => it.VisualElement.RemoveFromHierarchy());
        foreach (char c in characters.ToLowerInvariant())
        {
            CreateCharacter(c);
        }
    }

    private void CreateCharacter(char character)
    {
        VisualElement visualElement = characterUi.CloneTree().Children().FirstOrDefault();
        CharacterQuickJumpCharacterControl characterQuickJumpCharacterControl = new CharacterQuickJumpCharacterControl(visualElement, character.ToString());
        injector.WithRootVisualElement(visualElement)
            .Inject(characterQuickJumpCharacterControl);

        SongMeta match = songSelectSceneUiControl.GetCharacterQuickJumpSongMeta(character);
        if (match == null)
        {
            characterQuickJumpCharacterControl.Enabled = false;
        }

        characterQuickJumpEntryControls.Add(characterQuickJumpCharacterControl);
        characterContainer.Add(visualElement);
    }
}
