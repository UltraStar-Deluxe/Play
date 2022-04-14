using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Analyzes each beat in the song editor.
 */
[RequireComponent(typeof(MicSampleRecorder))]
public class SongEditorMicPitchTracker : AbstractMicPitchTracker
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMeta songMeta;

    private int nextBeatToAnalyze;

    [Inject]
    private Settings songEditorSettings;

    private int MicDelayInMillis => songEditorSettings.SongEditorSettings.MicDelayInMillis;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();

        songAudioPlayer.PlaybackStartedEventStream.Subscribe(positionInSongInMillis => UpdateNextBeatToAnalyze(positionInSongInMillis));
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(positionInSongInMillis => UpdateNextBeatToAnalyze(positionInSongInMillis));
        songAudioPlayer.JumpBackInSongEventStream.Subscribe(previousAndCurrentPositionInSongInMillis =>
            UpdateNextBeatToAnalyze(previousAndCurrentPositionInSongInMillis.Current));
        songAudioPlayer.JumpForwardInSongEventStream.Subscribe(previousAndCurrentPositionInSongInMillis =>
            UpdateNextBeatToAnalyze(previousAndCurrentPositionInSongInMillis.Current));
    }

    private void UpdateNextBeatToAnalyze(double positionInSongInMillis)
    {
        nextBeatToAnalyze = (int)BpmUtils.MillisecondInSongToBeat(songMeta, positionInSongInMillis);
    }

    protected override void OnRecordingEvent(RecordingEvent recordingEvent)
    {
        int firstBeatToAnalyze = nextBeatToAnalyze;
        float positionInSongInMillisConsideringMicDelay = (float)songAudioPlayer.PositionInSongInMillis - MicDelayInMillis;
        int currentBeatConsideringMicDelay = (int)BpmUtils.MillisecondInSongToBeat(songMeta, positionInSongInMillisConsideringMicDelay);
        for (int beatToAnalyze = firstBeatToAnalyze; beatToAnalyze < currentBeatConsideringMicDelay; beatToAnalyze++)
        {
            AnalyzeBeatAndNotify(beatToAnalyze);
            nextBeatToAnalyze = beatToAnalyze + 1;
        }
    }

    private void AnalyzeBeatAndNotify(int beat)
    {
        PitchEvent pitchEvent = AnalyzeBeat(
            songMeta,
            beat,
            songAudioPlayer.PositionInSongInMillis,
            MicSampleRecorder.MicProfile,
            MicSampleRecorder.MicSamples,
            audioSamplesAnalyzer);

        // Notify listeners
        if (pitchEvent == null)
        {
            pitchEventStream.OnNext(null);
        }
        else
        {
            int shiftedMidiNote = pitchEvent.MidiNote + (songEditorSettings.SongEditorSettings.MicOctaveOffset * 12);
            pitchEventStream.OnNext(new BeatPitchEvent(shiftedMidiNote, beat));
        }
    }
}
