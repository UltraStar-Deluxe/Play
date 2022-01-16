using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
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
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private UltraStarPlayInputManager inputManager;

    [Inject(UxmlName = R.UxmlNames.characterContainer)]
    private ScrollView characterContainer;

    [Inject(UxmlName = R.UxmlNames.previousCharacterButton)]
    private Button previousCharacterButton;

    [Inject(UxmlName = R.UxmlNames.nextCharacterButton)]
    private Button nextCharacterButton;

    private bool needsRefresh;

    private readonly List<CharacterQuickJumpCharacterControl> characterQuickJumpEntryControls = new List<CharacterQuickJumpCharacterControl>();
    private List<CharacterQuickJumpCharacterControl> EnabledCharacterQuickJumpEntryControls => characterQuickJumpEntryControls
        .Where(it => it.Enabled)
        .ToList();

    private void Start()
    {
        characterContainer.Clear();
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => UpdateCharacters()));

        previousCharacterButton.RegisterCallbackButtonTriggered(() => characterContainer.horizontalScroller.ScrollPageUp());
        nextCharacterButton.RegisterCallbackButtonTriggered(() => characterContainer.horizontalScroller.ScrollPageDown());

        // Update outdated song metas on main thread
        songMetaManager.SongScanFinishedEventStream
            .Subscribe(_ => needsRefresh = true);
        songSelectSceneControl.PlaylistChooserControl.Selection
            .Subscribe(_ => needsRefresh = true);

        UpdateNextAndPreviousCharacterButtonLabels();
        inputManager.InputDeviceChangeEventStream.Subscribe(evt => UpdateNextAndPreviousCharacterButtonLabels());
    }

    private void UpdateNextAndPreviousCharacterButtonLabels()
    {
        if (inputManager.InputDeviceEnum == EInputDevice.Gamepad)
        {
            nextCharacterButton.text = "R2 >";
            previousCharacterButton.text = "< L2";
        }
        else
        {
            nextCharacterButton.text = ">";
            previousCharacterButton.text = "<";
        }
    }

    private void Update()
    {
        if (needsRefresh)
        {
            needsRefresh = false;
            songSelectSceneControl.InitSongMetas();
            songSelectSceneControl.UpdateFilteredSongs();
            UpdateCharacters();
        }
    }

    public void SelectNextCharacter()
    {
        SelectAdjacentCharacter(true);
    }

    public void SelectPreviousCharacter()
    {
        SelectAdjacentCharacter(false);
    }

    private void SelectAdjacentCharacter(bool selectNext)
    {
        if (songSelectSceneControl.SelectedSong == null)
        {
            return;
        }

        char currentCharacter = GetCharacterQuickJumpRelevantString(songSelectSceneControl.SelectedSong)
            .ToLowerInvariant()
            .FirstOrDefault();
        CharacterQuickJumpCharacterControl currentCharacterQuickJumpCharacterControl = EnabledCharacterQuickJumpEntryControls
            .FirstOrDefault(it => it.Character == currentCharacter
                                  || (char.IsDigit(currentCharacter) && it.Character == '#')
                                  || (!char.IsLetterOrDigit(currentCharacter) && it.Character == '&'));
        if (currentCharacterQuickJumpCharacterControl == null)
        {
            return;
        }

        CharacterQuickJumpCharacterControl characterQuickJumpCharacterControl = selectNext
            ? EnabledCharacterQuickJumpEntryControls.GetElementAfter(currentCharacterQuickJumpCharacterControl, true)
            : EnabledCharacterQuickJumpEntryControls.GetElementBefore(currentCharacterQuickJumpCharacterControl, true);
        DoCharacterQuickJump(characterQuickJumpCharacterControl.Character);

        // Scroll the CharacterQuickJumpButton into view and animate it to make clear what happened
        VisualElement target = characterQuickJumpCharacterControl.VisualElement;
        characterContainer.ScrollTo(target);
        LeanTween.value(gameObject, Vector3.one * 0.5f, Vector3.one, 0.5f)
            .setEaseSpring()
            .setOnUpdate(s => target.style.scale = new StyleScale(new Scale(new Vector3(s, s, s))));
    }

    public void UpdateCharacters()
    {
        characterQuickJumpEntryControls.ForEach(it => it.VisualElement.RemoveFromHierarchy());
        characterQuickJumpEntryControls.Clear();
        foreach (char c in characters.ToLowerInvariant())
        {
            CreateCharacter(c);
        }
    }

    private void CreateCharacter(char character)
    {
        VisualElement visualElement = characterUi.CloneTree().Children().FirstOrDefault();
        CharacterQuickJumpCharacterControl characterQuickJumpCharacterControl = new CharacterQuickJumpCharacterControl(visualElement, character);
        injector.WithRootVisualElement(visualElement)
            .Inject(characterQuickJumpCharacterControl);

        SongMeta match = GetCharacterQuickJumpSongMeta(character);
        if (match == null)
        {
            characterQuickJumpCharacterControl.Enabled = false;
        }

        characterQuickJumpEntryControls.Add(characterQuickJumpCharacterControl);
        characterContainer.Add(visualElement);
    }

    public SongMeta GetCharacterQuickJumpSongMeta(char character)
    {
        Predicate<char> matchPredicate;
        if (char.IsLetterOrDigit(character))
        {
            // Jump to song starts with character
            matchPredicate = (songCharacter) => songCharacter == character;
        }
        else if (character == '#')
        {
            // Jump to song starts with number
            matchPredicate = (songCharacter) => char.IsDigit(songCharacter);
        }
        else
        {
            // Jump to song starts with non-alphanumeric character
            matchPredicate = (songCharacter) => !char.IsLetterOrDigit(songCharacter);
        }

        SongMeta match = songSelectSceneControl.GetFilteredSongMetas()
            .Where(songMeta =>
            {
                string relevantString = GetCharacterQuickJumpRelevantString(songMeta);
                return !relevantString.IsNullOrEmpty()
                       && matchPredicate.Invoke(relevantString.ToLowerInvariant()[0]);
            })
            .FirstOrDefault();

        return match;
    }

    private string GetCharacterQuickJumpRelevantString(SongMeta songMeta)
    {
        ESongOrder songOrder = songSelectSceneControl.SongOrderPickerControl.SelectedItem;
        switch (songOrder)
        {
            case ESongOrder.Artist:
                return songMeta.Artist;
            case ESongOrder.Title:
                return songMeta.Title;
            case ESongOrder.Genre:
                return songMeta.Genre;
            case ESongOrder.Language:
                return songMeta.Language;
            case ESongOrder.Folder:
                return songMeta.Directory + "/" + songMeta.Filename;
            default:
                return songMeta.Artist;
        }
    }

    public void DoCharacterQuickJump(char character)
    {
        SongMeta match = GetCharacterQuickJumpSongMeta(character);
        if (match != null)
        {
            songRouletteControl.SelectSong(match);
        }
    }
}
