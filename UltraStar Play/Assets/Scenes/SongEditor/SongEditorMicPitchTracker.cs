using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;

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

    private int MicDelayInMillis => settings.SongEditorSettings.MicDelayInMillis;

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
            AnalyzeBeat(beatToAnalyze);
            nextBeatToAnalyze = beatToAnalyze + 1;
        }
    }

    private void AnalyzeBeat(int beat)
    {
        float beatStartInMillis = (float)BpmUtils.BeatToMillisecondsInSong(songMeta, beat);
        float beatEndInMillis = (float)BpmUtils.BeatToMillisecondsInSong(songMeta, beat + 1);
        float beatLengthInMillis = beatEndInMillis - beatStartInMillis;
        int beatLengthInSamples = (int)(beatLengthInMillis * MicSampleRecorder.SampleRateHz / 1000f);

        // The newest sample in the buffer corresponds to (positionInSong - micDelay)
        float positionInSongInMillisConsideringMicDelay = (float)songAudioPlayer.PositionInSongInMillis - MicDelayInMillis;
        float distanceToNewestSamplesInMillis = positionInSongInMillisConsideringMicDelay - beatEndInMillis;
        int distanceToNewestSamplesInSamples = (int)(distanceToNewestSamplesInMillis * MicSampleRecorder.SampleRateHz / 1000f);
        distanceToNewestSamplesInSamples = NumberUtils.Limit(distanceToNewestSamplesInSamples, 0, MicSampleRecorder.MicSamples.Length - 1);

        int endIndex = MicSampleRecorder.MicSamples.Length - distanceToNewestSamplesInSamples;
        int startIndex = endIndex - beatLengthInSamples;
        endIndex = NumberUtils.Limit(endIndex, 0, MicSampleRecorder.MicSamples.Length - 1);
        startIndex = NumberUtils.Limit(startIndex, 0, MicSampleRecorder.MicSamples.Length - 1);
        if (endIndex < startIndex)
        {
            Debug.LogWarning($"Cannot analyze from sample {startIndex} to {endIndex}. Start index must be smaller than end index.");
            return;
        }

        PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(MicSampleRecorder.MicSamples, startIndex, endIndex, MicProfile);

        // Notify listeners
        if (pitchEvent == null)
        {
            pitchEventStream.OnNext(null);
        }
        else
        {
            int shiftedMidiNote = pitchEvent.MidiNote + (settings.SongEditorSettings.MicOctaveOffset * 12);
            pitchEventStream.OnNext(new BeatPitchEvent(shiftedMidiNote, beat));
        }
    }
}
