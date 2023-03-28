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
    
    [Inject(UxmlName = R.UxmlNames.midiTrackIndexPicker)]
    private ItemPicker midiTrackIndexPicker;
    
    [Inject(UxmlName = R.UxmlNames.previewMidiTrackAndChannelButton)]
    private Button previewMidiTrackAndChannelButton;
    
    [Inject(UxmlName = R.UxmlNames.midiLyricsTextField)]
    private TextField midiLyricsTextField;
    
    [Inject(UxmlName = R.UxmlNames.importWithLyricsToggle)]
    private Toggle importWithLyricsToggle;
    
    [Inject(UxmlName = R.UxmlNames.assignToPlayerPicker)]
    private ItemPicker assignToPlayerPicker;
    
    [Inject(UxmlName = R.UxmlNames.closeImportMidiDialogButton)]
    private Button closeImportMidiDialogButton;
    
    [Inject(UxmlName = R.UxmlNames.importMidiFileDialogButton)]
    private Button importMidiFileDialogButton;
    
    [Inject]
    private MidiManager midiManager;

    private readonly SongEditorMidiFileImporter midiFileImporter = new();

    private LabeledItemPickerControl<TrackAndChannel> midiTrackIndexPickerControl;
    private LabeledItemPickerControl<int> midiAssignToPlayerPickerControl;

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
        injector.Inject(midiFileImporter);
        
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

        midiTrackIndexPickerControl = new(midiTrackIndexPicker, new List<TrackAndChannel>());
        midiTrackIndexPickerControl.AutoSmallFont = false;
        midiTrackIndexPickerControl.Selection.Subscribe(_ =>
        {
            StopPreview();
            UpdateMidiLyrics();
        });
        new AutoFitLabelControl(midiTrackIndexPicker.ItemLabel);
        
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
            int trackIndex = midiTrackIndexPickerControl.SelectedItem.trackIndex;
            int channelIndex = midiTrackIndexPickerControl.SelectedItem.channelIndex;
            MidiFile midiFileCopy = midiManager.LoadMidiFile(MidiFilePath);
            
            MidiFileUtils.CalculateMidiEventTimesInMillis(
                midiFileCopy,
                out Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
                out Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis);

            List<Note> loadNotesFromMidiFile = midiFileImporter.LoadNotesFromMidiFile(midiFileCopy, trackIndex, channelIndex, false, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
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
            midiTrackIndexPickerControl.SelectedItem.trackIndex,
            midiTrackIndexPickerControl.SelectedItem.channelIndex,
            importWithLyricsToggle.value,
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
        UpdateMidiLyrics();
    }

    private void UpdateMidiLyrics()
    {
        if (SelectedTrack == null)
        {
            midiLyricsTextField.value = "";
            importWithLyricsToggle.SetEnabled(false);
            return;
        }

        string lyrics = MidiFileUtils.GetLyrics(SelectedTrack);
        midiLyricsTextField.value = lyrics.IsNullOrEmpty()
            ? "No lyrics found"
            : lyrics;
        importWithLyricsToggle.SetEnabled(!lyrics.IsNullOrEmpty());
        importWithLyricsToggle.value = !lyrics.IsNullOrEmpty();
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
        List<TrackAndChannel> trackAndChannels = new();
        List<int> trackIndexes = MidiFileUtils.GetTrackIndexes(midiFile);
        trackIndexes.ForEach(trackIndex =>
        {
            MidiTrack track = midiFile.Tracks[trackIndex];
            List<int> channelIndexes = MidiFileUtils.GetChannelIndexes(track, true);
            channelIndexes.ForEach(channelIndex =>
            {
                trackAndChannels.Add(new (trackIndex, channelIndex));
            });
        });

        midiTrackIndexPickerControl.Items = trackAndChannels;
        
        TrackAndChannel bestMatchingTrackAndChannel = FindBestMatchingTrackAndChannel(trackAndChannels);
        if (bestMatchingTrackAndChannel != null)
        {
            midiTrackIndexPickerControl.SelectItem(bestMatchingTrackAndChannel);
        }
        else
        {
            midiTrackIndexPickerControl.SelectItem(trackAndChannels.FirstOrDefault());
        }
    }

    private TrackAndChannel FindBestMatchingTrackAndChannel(List<TrackAndChannel> trackAndChannels)
    {
        // TODO: Bad performance
        
        if (trackAndChannels.IsNullOrEmpty())
        {
            return null;
        }
        
        using DisposableStopwatch d = new DisposableStopwatch("FindBestMatchingTrackAndChannel took <ms>");
        
        MidiFileUtils.CalculateMidiEventTimesInMillis(
            midiFile,
            out Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
            out Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis);

        int GetAbsoluteDeltaTimeInMillis(MidiEvent midiEvent)
        {
            if (midiEventToAbsoluteDeltaTimeInMillis.TryGetValue(midiEvent, out int absoluteDeltaTimeInMillis))
            {
                return absoluteDeltaTimeInMillis;
            }

            return 0;
        }
        
        int FindTrackIndexWithLongestLyrics()
        {
            if (trackAndChannels.IsNullOrEmpty())
            {
                return -1;
            }
            if (trackAndChannels.Count == 1)
            {
                return 0;
            }
            
            int trackIndexWithLongestLyrics = trackAndChannels.FirstOrDefault().trackIndex;
            int longestLyricsLength = 0;
            foreach (TrackAndChannel trackAndChannel in trackAndChannels)
            {
                MidiTrack midiTrack = midiFile.Tracks[trackAndChannel.trackIndex];
                string lyrics = MidiFileUtils.GetLyrics(midiTrack);
                if (!lyrics.IsNullOrEmpty()
                    && lyrics.Length > longestLyricsLength)
                {
                    trackIndexWithLongestLyrics = trackAndChannel.trackIndex;
                    longestLyricsLength = lyrics.Length;
                }
            }

            return trackIndexWithLongestLyrics;
        }

        int bestTrackIndex = FindTrackIndexWithLongestLyrics();
        if (bestTrackIndex < 0)
        {
            return trackAndChannels.FirstOrDefault();
        }

        double GetMidiEventAbsoluteTimeDistance(MidiEvent a, MidiEvent b)
        {
            if (a == null
                && b == null)
            {
                return 0;
            }

            if (a == null)
            {
                return GetAbsoluteDeltaTimeInMillis(b);
            }

            if (b == null)
            {
                return GetAbsoluteDeltaTimeInMillis(a);
            }
            
            return Mathf.Abs(GetAbsoluteDeltaTimeInMillis(a) - GetAbsoluteDeltaTimeInMillis(b));
        }
        
        int FindChannelIndexWithBestMatchingNotes()
        {
            List<int> channelIndexes = trackAndChannels
                .Where(it => it.trackIndex == bestTrackIndex)
                .Select(it => it.channelIndex)
                .Distinct()
                .ToList();
            if (channelIndexes.IsNullOrEmpty())
            {
                return -1;
            }
            if (channelIndexes.Count == 1)
            {
                return channelIndexes[0];
            }
            
            // For each channel, calculate difference to lyrics events. Return the channel with smallest difference.
            MidiTrack bestTrack = midiFile.Tracks[bestTrackIndex];
            List<MidiEvent> lyricsEvents = MidiFileUtils.GetLyricsEvents(bestTrack);
            
            Dictionary<int, double> channelIndexToDistance = new();
            foreach (int channelIndex in channelIndexes)
            {
                List<MidiEvent> noteEventsOfChannel = bestTrack.MidiEvents
                    .Where(midiEvent => midiEvent.Channel == (byte)channelIndex
                                        && midiEvent.TryGetMidiEventTypeEnum(out MidiEventTypeEnum midiEventTypeEnum)
                                            && midiEventTypeEnum == MidiEventTypeEnum.NoteOn)
                    .ToList();
            
                double distanceOfChannel = 0;
                foreach (MidiEvent lyricsEvent in lyricsEvents)
                {
                    MidiEvent closestNoteOfLyricsEvent = noteEventsOfChannel.FindMinElement(noteEvent =>
                        GetMidiEventAbsoluteTimeDistance(lyricsEvent, noteEvent));
                    if (closestNoteOfLyricsEvent == null)
                    {
                        // Add unmatched lyrics event to distance.
                        distanceOfChannel += GetAbsoluteDeltaTimeInMillis(lyricsEvent);
                    }
            
                    noteEventsOfChannel.Remove(closestNoteOfLyricsEvent);
                    double distanceOfNote = GetMidiEventAbsoluteTimeDistance(lyricsEvent, closestNoteOfLyricsEvent);
                    distanceOfChannel += distanceOfNote;
                }
            
                // Add unmatched notes to distance
                distanceOfChannel += noteEventsOfChannel.Sum(noteEvent => GetAbsoluteDeltaTimeInMillis(noteEvent));
                
                channelIndexToDistance[channelIndex] = distanceOfChannel;
            }
            
            int channelIndexWithSmallestDistance = channelIndexToDistance.FindMinElement(entry => entry.Value).Key;
            return channelIndexWithSmallestDistance;
        }

        int bestChannelIndex = FindChannelIndexWithBestMatchingNotes();
        if (bestChannelIndex < 0)
        {
            return null;
        }
        return new TrackAndChannel(bestTrackIndex, bestChannelIndex);
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

    public class TrackAndChannel
    {
        public int trackIndex;
        public int channelIndex;
        
        public TrackAndChannel(int trackIndex, int channelIndex)
        {
            this.trackIndex = trackIndex;
            this.channelIndex = channelIndex;
        }

        public override string ToString()
        {
            return $"track {trackIndex}, channel {channelIndex}";
        }
    }
}
