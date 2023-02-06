using System;
using System.Collections.Generic;
using System.IO;
using CSharpSynth.Midi;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class ImportMidiFileDialogControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Injector injector;
    
    [Inject]
    private Settings settings;
    
    [Inject]
    private SongMeta songMeta;
    
    [Inject(UxmlName = R.UxmlNames.importMidiFileDialogOverlay)]
    private VisualElement importMidiFileDialogOverlay;
    
    [Inject(UxmlName = R.UxmlNames.midiFilePathTextField)]
    private TextField midiFilePathTextField;
    
    [Inject(UxmlName = R.UxmlNames.midiFileIssueContainer)]
    private VisualElement midiFileIssueContainer;
    
    [Inject(UxmlName = R.UxmlNames.midiFileIssueLabel)]
    private Label midiFileIssueLabel;
    
    [Inject(UxmlName = R.UxmlNames.midiTrackIndexPicker)]
    private ItemPicker midiTrackIndexPicker;
    
    [Inject(UxmlName = R.UxmlNames.midiChannelIndexPicker)]
    private ItemPicker midiChannelIndexPicker;
    
    [Inject(UxmlName = R.UxmlNames.previewMidiTrackAndChannelButton)]
    private Button previewMidiTrackAndChannelButton;
    
    [Inject(UxmlName = R.UxmlNames.midiLyricsTextField)]
    private TextField midiLyricsTextField;
    
    [Inject(UxmlName = R.UxmlNames.importWithoutLyricsToggle)]
    private Toggle importWithoutLyricsToggle;
    
    [Inject(UxmlName = R.UxmlNames.assignToPlayerPicker)]
    private ItemPicker assignToPlayerPicker;
    
    [Inject(UxmlName = R.UxmlNames.closeImportMidiDialogButton)]
    private Button closeImportMidiDialogButton;
    
    [Inject(UxmlName = R.UxmlNames.importMidiFileDialogButton)]
    private Button importMidiFileDialogButton;
    
    [Inject]
    private MidiManager midiManager;

    private readonly SongEditorMidiFileImporter midiFileImporter = new();

    private LabeledItemPickerControl<int> midiTrackIndexPickerControl;
    private LabeledItemPickerControl<int> midiChannelIndexPickerControl;
    private LabeledItemPickerControl<int> midiAssignToPlayerPickerControl;

    private MidiFile midiFile;
    private MidiTrack SelectedTrack
    {
        get
        {
            if (midiFile == null)
            {
                return null;
            }

            int selectedTrackIndex = midiTrackIndexPickerControl.SelectedItem;
            if (selectedTrackIndex >= 0 && selectedTrackIndex < midiFile.Tracks.Length)
            {
                return midiFile.Tracks[selectedTrackIndex];
            }
            return null;
        }
    }

    private string MidiFilePath
    {
        get
        {
            return midiFilePathTextField.value;
        }
        set
        {
            midiFilePathTextField.value = value;
            settings.SongEditorSettings.LastMidiFilePath = value;
            UpdateControls();
        }
    }

    public void OnInjectionFinished()
    {
        injector.Inject(midiFileImporter);
        
        closeImportMidiDialogButton.RegisterCallbackButtonTriggered(() => CloseDialog());
        previewMidiTrackAndChannelButton.RegisterCallbackButtonTriggered(() =>
        {
            if (midiManager.IsPlayingMidiFile)
            {
                StopPreview();
            }
            else
            {
                StartPreview();
            }
        });
        importMidiFileDialogButton.RegisterCallbackButtonTriggered(() =>
        {
            ImportMidiFile();
            CloseDialog();
        });
        VisualElementUtils.RegisterCallbackToHideByDisplayOnDirectClick(importMidiFileDialogOverlay, CloseDialog);

        midiTrackIndexPickerControl = new(midiTrackIndexPicker, new List<int>());
        midiTrackIndexPickerControl.Selection.Subscribe(_ => StopPreview());
        midiChannelIndexPickerControl = new(midiChannelIndexPicker, new List<int>());
        midiChannelIndexPickerControl.Selection.Subscribe(_ => StopPreview());
        
        midiAssignToPlayerPickerControl = new(assignToPlayerPicker, new List<int> { -1, 0, 1 });
        midiAssignToPlayerPickerControl.GetLabelTextFunction = newValue =>
        {
            return newValue switch
            {
                0 => "Player 01",
                1 => "Player 02",
                _ => "None"
            };
        };
        midiAssignToPlayerPickerControl.SelectItem(-1);

        midiFilePathTextField.RegisterValueChangedCallback(evt => UpdateControls());
        
        CloseDialog();
    }

    private void StopPreview()
    {
        if (!midiManager.IsPlayingMidiFile)
        {
            return;
        }
        
        previewMidiTrackAndChannelButton.text = "Start Preview";
        Debug.Log("Stopping preview of midi file");
        
        midiManager.StopMidiFile();
    }

    private void StartPreview()
    {
        if (SelectedTrack == null
            || midiManager.IsPlayingMidiFile)
        {
            return;
        }

        previewMidiTrackAndChannelButton.text = "Stop Preview";
        Debug.Log("Starting preview of midi file");
        
        try
        {
            int trackIndex = midiTrackIndexPickerControl.SelectedItem;
            int channelIndex = midiChannelIndexPickerControl.SelectedItem;
            MidiFile midiFileCopy = midiManager.LoadMidiFile(MidiFilePath);
            List<Note> loadNotesFromMidiFile = midiFileImporter.LoadNotesFromMidiFile(midiFileCopy, trackIndex, channelIndex, true);
            MidiFile previewMidiFile = MidiFileUtils.CreateMidiFile(songMeta, loadNotesFromMidiFile, (byte)settings.SongEditorSettings.MidiVelocity);
            MidiFileUtils.SetFirstDeltaTimeTo(previewMidiFile, 0);
            midiManager.PlayMidiFile(previewMidiFile);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            UiManager.CreateNotification($"Preview failed: {e.Message}");
            throw;
        }
    }

    private void ImportMidiFile()
    {
        if (!FileUtils.Exists(MidiFilePath))
        {
            UiManager.CreateNotification("File does not exist");
            return;
        }

        StopPreview();
        
        string voiceName = null;
        if (midiAssignToPlayerPickerControl.SelectedItem == 0)
        {
            voiceName = Voice.firstVoiceName;
        }
        else if (midiAssignToPlayerPickerControl.SelectedItem == 1)
        {
            voiceName = Voice.secondVoiceName;
        }
        
        midiFileImporter.ImportMidiFile(
            midiFilePathTextField.value,
            midiTrackIndexPickerControl.SelectedItem,
            midiChannelIndexPickerControl.SelectedItem,
            importWithoutLyricsToggle.value,
            voiceName);
    }
    
    public void OpenDialog()
    {
        MidiFilePath = settings.SongEditorSettings.LastMidiFilePath;
        importMidiFileDialogOverlay.ShowByDisplay();
    }

    public void CloseDialog()
    {
        StopPreview();
        importMidiFileDialogOverlay.HideByDisplay();
    }
    
    private void UpdateControls()
    {
        string errorMessage = GetMidiFileErrorMessage();
        SetErrorMessage(errorMessage);
        if (!errorMessage.IsNullOrEmpty())
        {
            return;
        }

        try
        {
            midiFile = midiFileImporter.LoadMidiFile(MidiFilePath);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetErrorMessage($"Import failed: {e.Message}");
            return;
        }
        
        UpdateTrackIndexPicker();
        UpdateChannelIndexPicker();
        UpdateMidiLyrics();
    }

    private void UpdateMidiLyrics()
    {
        if (SelectedTrack == null)
        {
            midiLyricsTextField.value = "";
            return;
        }

        string lyrics = MidiFileUtils.GetLyrics(SelectedTrack);
        midiLyricsTextField.value = lyrics;
    }

    private void SetErrorMessage(string errorMessage)
    {
        bool hasError = !errorMessage.IsNullOrEmpty();
        midiFileIssueContainer.SetVisibleByDisplay(hasError);
        midiFileIssueLabel.text = errorMessage;
        importMidiFileDialogButton.SetEnabled(!hasError);
    }

    private void UpdateTrackIndexPicker()
    {
        List<int> trackIndexes = MidiFileUtils.GetTrackIndexes(midiFile);
        midiTrackIndexPickerControl.Items = trackIndexes;
    }

    private void UpdateChannelIndexPicker()
    {
        if (SelectedTrack == null)
        {
            midiChannelIndexPickerControl.Items = new List<int>();
            return;
        }

        List<int> channelIndexes = MidiFileUtils.GetChannelIndexes(SelectedTrack);
        midiChannelIndexPickerControl.Items = channelIndexes;
    }
    
    private string GetMidiFileErrorMessage()
    {
        if (MidiFilePath.IsNullOrEmpty())
        {
            return "Enter path to MIDI or KAR file";
        }
        
        if (!FileUtils.Exists(MidiFilePath))
        {
            return "File does not exist";
        }

        List<string> supportedFileExtensions = new() { ".mid", ".midi", ".kar" };
        string midiFileExtension = Path.GetExtension(MidiFilePath.ToLowerInvariant());
        if (!supportedFileExtensions.Contains(midiFileExtension))
        {
            return "Unsupported file format";
        }
        
        return "";
    }
}
