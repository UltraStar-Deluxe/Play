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

    private DropdownFieldControl<TrackAndChannel> midiTrackIndexChooserControl;
    private DropdownFieldControl<EVoiceId> midiAssignToPlayerChooserControl;

    private MidiFile midiFile;
    private MidiTrack SelectedTrack
    {
        get
        {
            if (midiFile == null
                || midiTrackIndexChooserControl.Selection == null)
            {
                return null;
            }

            int selectedTrackIndex = midiTrackIndexChooserControl.Selection.trackIndex;
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

        midiTrackIndexChooserControl = new(trackAndChannelDropdownField, new List<TrackAndChannel>(), null,
            trackAndChannel => GetDisplayName(trackAndChannel));
        midiTrackIndexChooserControl.SelectionAsObservable.Subscribe(_ =>
        {
            bool wasPlaying = midiManager.IsPlayingMidiFile;
            StopPreview();
            if (wasPlaying)
            {
                StartPreview();
            }
        });

        midiAssignToPlayerChooserControl = new(assignToPlayerDropdownField, EnumUtils.GetValuesAsList<EVoiceId>(), EVoiceId.P1, voice => GetVoiceDisplayName(voice));
        midiAssignToPlayerChooserControl.Selection = EVoiceId.P1;

        midiFilePathTextField.DisableParseEscapeSequences();
        midiFilePathTextField.RegisterValueChangedCallback(evt => UpdateControls());

        midiLyricsTextField.DisableParseEscapeSequences();

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

    private string GetVoiceDisplayName(EVoiceId voiceId)
    {
        if (voiceId is EVoiceId.P1)
        {
            return "Player 1";
        }

        if (voiceId is EVoiceId.P2)
        {
            return "Player 2";
        }

        return voiceId.ToString();
    }

    private void OpenMidiFileDialog()
    {
        FileSystemDialogUtils.OpenFileDialogToSetPath(
            "Select Midi File",
            SongMetaUtils.GetDirectoryPath(songMeta),
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
            int trackIndex = midiTrackIndexChooserControl.Selection.trackIndex;
            int channelIndex = midiTrackIndexChooserControl.Selection.channelIndex;
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
        catch (Exception ex)
        {
            Debug.LogException(ex);
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                "reason", ex.Message));
            throw;
        }
    }

    private void ImportMidiFile()
    {
        if (!FileUtils.Exists(MidiFilePath))
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error_fileNotFound));
            return;
        }

        StopPreview();

        EVoiceId voiceId;
        if (assignToPlayerToggle.value
            && midiAssignToPlayerChooserControl.Selection is EVoiceId.P1)
        {
            voiceId = EVoiceId.P1;
        }
        else if (assignToPlayerToggle.value
                 && midiAssignToPlayerChooserControl.Selection is EVoiceId.P2)
        {
            voiceId = EVoiceId.P2;
        }
        else
        {
            voiceId = EVoiceId.P1;
        }

        midiFileImporter.ImportMidiFile(
            midiFilePathTextField.value,
            midiTrackIndexChooserControl.Selection.trackIndex,
            midiTrackIndexChooserControl.Selection.channelIndex,
            importMidiLyricsToggle.value,
            importMidiNotesToggle.value,
            voiceId,
            true,
            ESongEditorLayer.Import);
        NotificationManager.CreateNotification(Translation.Get(R.Messages.songEditor_midiImportDialog_success));
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
        Translation errorMessage = GetMidiFileErrorMessage();
        SetErrorMessage(errorMessage);
        if (!errorMessage.Value.IsNullOrEmpty())
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
            SetErrorMessage(Translation.Get(R.Messages.common_errorWithReason, "reason", e.Message));
            return;
        }

        UpdateTrackIndexChooser();
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
            bestMatchnigTrackAndChannelLabel.SetTranslatedText(Translation.Empty);
        }
        else
        {
            midiLyricsTextField.value = lyrics;
        }
        importMidiLyricsToggle.SetEnabled(!lyrics.IsNullOrEmpty());
        importMidiLyricsToggle.value = !lyrics.IsNullOrEmpty();
    }

    private void SetErrorMessage(Translation errorMessage)
    {
        bool hasError = !errorMessage.Value.IsNullOrEmpty();
        midiFileIssueContainer.SetVisibleByDisplay(hasError);
        midiFileIssueLabel.SetTranslatedText(errorMessage);
        importMidiFileDialogButton.SetEnabled(!hasError);
    }

    private void UpdateTrackIndexChooser()
    {
        List<TrackAndChannel> trackAndChannels = MidiFileUtils.GetTracksAndChannels(midiFile);
        midiTrackIndexChooserControl.Items = trackAndChannels;

        MidiFileUtils.CalculateMidiEventTimesInMillis(
            midiFile,
            out Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
            out Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis);

        List<MidiEvent> lyricsEvents = MidiFileUtils.GetLyricsEvents(midiFile);
        TrackAndChannel bestMatchingTrackAndChannel = MidiToSongMetaUtils.FindTrackAndChannelWithBestMatchingNotesForLyricsEvents(midiFile, lyricsEvents, trackAndChannels, midiEventToAbsoluteDeltaTimeInMillis);
        if (bestMatchingTrackAndChannel != null)
        {
            bestMatchnigTrackAndChannelLabel.SetTranslatedText(Translation.Get(R.Messages.songEditor_midiImportDialog_bestMatchingTrack,
                "value", bestMatchingTrackAndChannel));
            midiTrackIndexChooserControl.Selection = bestMatchingTrackAndChannel;
        }
        else
        {
            bestMatchnigTrackAndChannelLabel.SetTranslatedText(Translation.Empty);
            midiTrackIndexChooserControl.Selection = trackAndChannels.FirstOrDefault();
        }
    }

    private Translation GetMidiFileErrorMessage()
    {
        if (MidiFilePath.IsNullOrEmpty())
        {
            return Translation.Get(R.Messages.songEditor_midiImportDialog_error_missingPath);
        }

        if (!FileUtils.Exists(MidiFilePath))
        {
            return Translation.Get(R.Messages.songEditor_midiImportDialog_error_fileNotFound);
        }

        List<string> supportedFileExtensions = new() { ".mid", ".midi", ".kar" };
        string midiFileExtension = Path.GetExtension(MidiFilePath.ToLowerInvariant());
        if (!supportedFileExtensions.Contains(midiFileExtension))
        {
            return Translation.Get(R.Messages.songEditor_midiImportDialog_error_unsupportedFormat);
        }

        return Translation.Empty;
    }
}
