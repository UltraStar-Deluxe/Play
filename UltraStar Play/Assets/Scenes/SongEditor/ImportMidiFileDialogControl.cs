using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AudioSynthesis.Midi;
using AudioSynthesis.Midi.Event;
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
    
    [Inject(UxmlName = R.UxmlNames.trackAndChannelDropdownField)]
    private DropdownField trackAndChannelDropdownField;
    
    [Inject(UxmlName = R.UxmlNames.bestMatchnigTrackAndChannelLabel)]
    private Label bestMatchnigTrackAndChannelLabel;
    
    [Inject(UxmlName = R.UxmlNames.startMidiPreviewIcon)]
    private VisualElement startMidiPreviewIcon;
    
    [Inject(UxmlName = R.UxmlNames.stopMidiPreviewIcon)]
    private VisualElement stopMidiPreviewIcon;
    
    [Inject(UxmlName = R.UxmlNames.previewMidiTrackAndChannelButton)]
    private Button previewMidiTrackAndChannelButton;
    
    [Inject(UxmlName = R.UxmlNames.midiLyricsTextField)]
    private TextField midiLyricsTextField;
    
    [Inject(UxmlName = R.UxmlNames.importMidiLyricsToggle)]
    private Toggle importMidiLyricsToggle;
    
    [Inject(UxmlName = R.UxmlNames.importMidiNotesToggle)]
    private Toggle importMidiNotesToggle;
    
    [Inject(UxmlName = R.UxmlNames.assignToPlayerToggle)]
    private Toggle assignToPlayerToggle;
    
    [Inject(UxmlName = R.UxmlNames.assignToPlayerDropdownField)]
    private DropdownField assignToPlayerDropdownField;
    
    [Inject(UxmlName = R.UxmlNames.closeImportMidiDialogButton)]
    private Button closeImportMidiDialogButton;
    
    [Inject(UxmlName = R.UxmlNames.importMidiFileDialogButton)]
    private Button importMidiFileDialogButton;
    
    [Inject(UxmlName = R.UxmlNames.selectMidiFileButton)]
    private Button selectMidiFileButton;
    
    [Inject]
    private MidiManager midiManager;

    [Inject]
    private SongEditorMidiFileImporter midiFileImporter;

    private DropdownFieldControl<TrackAndChannel> midiTrackIndexPickerControl;
    private DropdownFieldControl<EVoice> midiAssignToPlayerPickerControl;

    private MidiFile midiFile;
    private MidiTrack SelectedTrack
    {
        get
        {
            if (midiFile == null
                || midiTrackIndexPickerControl.SelectedItem == null)
            {
                return null;
            }

            int selectedTrackIndex = midiTrackIndexPickerControl.SelectedItem.trackIndex;
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
            return midiFilePathTextField.value.Trim();
        }
        set
        {
            midiFilePathTextField.value = value.Trim();
            settings.SongEditorSettings.LastMidiFilePath = value.Trim();
            UpdateControls();
        }
    }

    public void OnInjectionFinished()
    {
        stopMidiPreviewIcon.HideByDisplay();
        
        closeImportMidiDialogButton.RegisterCallbackButtonTriggered(_ => CloseDialog());
        previewMidiTrackAndChannelButton.RegisterCallbackButtonTriggered(_ =>
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
        importMidiFileDialogButton.RegisterCallbackButtonTriggered(_ =>
        {
            ImportMidiFile();
            CloseDialog();
        });
        VisualElementUtils.RegisterDirectClickCallback(importMidiFileDialogOverlay, CloseDialog);
        
        midiTrackIndexPickerControl = new(trackAndChannelDropdownField, new List<TrackAndChannel>(), null,
            trackAndChannel => GetDisplayName(trackAndChannel));
        midiTrackIndexPickerControl.Selection.Subscribe(_ =>
        {
            bool wasPlaying = midiManager.IsPlayingMidiFile;
            StopPreview();
            if (wasPlaying)
            {
                StartPreview();
            }
        });

        midiAssignToPlayerPickerControl = new(assignToPlayerDropdownField, EnumUtils.GetValuesAsList<EVoice>(), EVoice.P1, voice => GetDisplayName(voice));
        midiAssignToPlayerPickerControl.SetSelection(EVoice.P1);

        midiFilePathTextField.RegisterValueChangedCallback(evt => UpdateControls());

        if (PlatformUtils.IsStandalone)
        {
            selectMidiFileButton.RegisterCallbackButtonTriggered(_ => OpenMidiFileDialog());
        }
        else
        {
            selectMidiFileButton.HideByDisplay();
        }
        
        CloseDialog();
    }

    private string GetDisplayName(TrackAndChannel trackAndChannel)
    {
        MidiTrack track = midiFile.Tracks[trackAndChannel.trackIndex];
        string sequenceOrTrackName = MidiFileUtils.GetSequenceOrTrackName(track);
        string instrumentName = MidiFileUtils.GetInstrumentName(track, trackAndChannel.channelIndex);
        if (!sequenceOrTrackName.IsNullOrEmpty()
            && !instrumentName.IsNullOrEmpty())
        {
            return $"{trackAndChannel} ({sequenceOrTrackName}, {instrumentName})";
        }
        else if (!sequenceOrTrackName.IsNullOrEmpty())
        {
            return $"{trackAndChannel} ({sequenceOrTrackName})";
        }
        else if (!instrumentName.IsNullOrEmpty())
        {
            return $"{trackAndChannel} ({instrumentName})";
        }
        else
        {
            return $"{trackAndChannel}";
        }
    }
    
    private string GetDisplayName(EVoice voice)
    {
        if (voice is EVoice.P1)
        {
            return "Player 01";
        }
        else if (voice is EVoice.P2)
        {
            return "Player 02";
        }
        else
        {
            return ObjectUtils.NullableToString(voice, "-");
        }
    }

    private void OpenMidiFileDialog()
    {
        FileSystemDialogUtils.OpenFileDialogToSetPath(
            "Select Midi File",
            songMeta.Directory,
            FileSystemDialogUtils.CreateExtensionFilters("Midi Files", ApplicationUtils.supportedMidiFiles),
            () => MidiFilePath,
            newValue =>
            {
                MidiFilePath = newValue;
            });
    }

    private void StopPreview()
    {
        if (!midiManager.IsPlayingMidiFile)
        {
            return;
        }
        
        startMidiPreviewIcon.ShowByDisplay();
        stopMidiPreviewIcon.HideByDisplay();
        
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

        startMidiPreviewIcon.HideByDisplay();
        stopMidiPreviewIcon.ShowByDisplay();
        
        Debug.Log("Starting preview of midi file");
        try
        {
            int trackIndex = midiTrackIndexPickerControl.SelectedItem.trackIndex;
            int channelIndex = midiTrackIndexPickerControl.SelectedItem.channelIndex;
            MidiFile midiFileCopy = MidiFileUtils.LoadMidiFile(MidiFilePath);
            
            MidiFileUtils.CalculateMidiEventTimesInMillis(
                midiFileCopy,
                out Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
                out Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis);

            List<Note> loadNotesFromMidiFile = MidiToSongMetaUtils.LoadNotesFromMidiFile(songMeta, midiFileCopy, trackIndex, channelIndex, false, true, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
            MidiFile previewMidiFile = MidiFileUtils.CreateMidiFile(songMeta, loadNotesFromMidiFile, (byte)settings.SongEditorSettings.MidiVelocity);
            MidiFileUtils.SetFirstDeltaTimeTo(previewMidiFile, 0, 0);
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
        if (assignToPlayerToggle.value
            && midiAssignToPlayerPickerControl.SelectedItem is EVoice.P1)
        {
            voiceName = Voice.firstVoiceName;
        }
        else if (assignToPlayerToggle.value
                 && midiAssignToPlayerPickerControl.SelectedItem is EVoice.P2)
        {
            voiceName = Voice.secondVoiceName;
        }
        
        midiFileImporter.ImportMidiFile(
            midiFilePathTextField.value,
            midiTrackIndexPickerControl.SelectedItem.trackIndex,
            midiTrackIndexPickerControl.SelectedItem.channelIndex,
            importMidiLyricsToggle.value,
            importMidiNotesToggle.value,
            voiceName,
            true,
            ESongEditorLayer.Import);
        UiManager.CreateNotification("Loaded MIDI file successfully");
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
            midiFile = MidiFileUtils.LoadMidiFile(MidiFilePath);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetErrorMessage($"Import failed: {e.Message}");
            return;
        }
        
        UpdateTrackIndexPicker();
        UpdateMidiLyrics();
    }

    private void UpdateMidiLyrics()
    {
        if (SelectedTrack == null)
        {
            midiLyricsTextField.value = "";
            importMidiLyricsToggle.SetEnabled(false);
            return;
        }

        string lyrics = MidiFileUtils.GetLyrics(midiFile);
        if (lyrics.IsNullOrEmpty())
        {
            midiLyricsTextField.value = "No lyrics found";
            bestMatchnigTrackAndChannelLabel.text = "";
        }
        else
        {
            midiLyricsTextField.value = lyrics;
        }
        importMidiLyricsToggle.SetEnabled(!lyrics.IsNullOrEmpty());
        importMidiLyricsToggle.value = !lyrics.IsNullOrEmpty();
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
        List<TrackAndChannel> trackAndChannels = MidiFileUtils.GetTracksAndChannels(midiFile);
        midiTrackIndexPickerControl.Items = trackAndChannels;
        
        MidiFileUtils.CalculateMidiEventTimesInMillis(
            midiFile,
            out Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
            out Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis);

        List<MidiEvent> lyricsEvents = MidiFileUtils.GetLyricsEvents(midiFile);
        TrackAndChannel bestMatchingTrackAndChannel = MidiToSongMetaUtils.FindTrackAndChannelWithBestMatchingNotesForLyricsEvents(midiFile, lyricsEvents, trackAndChannels, midiEventToAbsoluteDeltaTimeInMillis);
        if (bestMatchingTrackAndChannel != null)
        {
            bestMatchnigTrackAndChannelLabel.text = $"Best matching notes: track {bestMatchingTrackAndChannel}";
            midiTrackIndexPickerControl.SetSelection(bestMatchingTrackAndChannel);
        }
        else
        {
            bestMatchnigTrackAndChannelLabel.text = $"";
            midiTrackIndexPickerControl.SetSelection(trackAndChannels.FirstOrDefault());
        }
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
