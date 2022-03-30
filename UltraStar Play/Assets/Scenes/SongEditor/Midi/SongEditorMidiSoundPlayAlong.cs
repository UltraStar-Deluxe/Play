using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorMidiSoundPlayAlong : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private MidiManager midiManager;

    [Inject]
    private Settings settings;

    [Inject]
    private Injector injector;

    private SongEditorMidiSoundPlayAlongThread thread;
    private float positionInSongInMillisOld;

    void Update()
    {
        if (!settings.SongEditorSettings.MidiSoundPlayAlongEnabled)
        {
            // Do not play midi sounds.
            // Furthermore, stop currently playing sounds if this setting changed during playback.
            if (thread != null)
            {
                StopThread();
            }
            return;
        }

        if (songAudioPlayer.IsPlaying && thread == null)
        {
            // Start thread for playing Midi note at correct time
            StartThread();
        }
        else if (!songAudioPlayer.IsPlaying && thread != null)
        {
            StopThread();
        }

        if (thread != null)
        {
            if (songAudioPlayer.PositionInSongInMillis < positionInSongInMillisOld)
            {
                // Jumped back, thus recalculate upcomingSortedNotes and stop any currently playing notes.
                thread.CalculateUpcomingSortedNotes((int)songAudioPlayer.PositionInSongInMillis);
                midiManager.StopAllMidiNotes();
            }

            thread.SynchronizeWithPlaybackPosition((int)songAudioPlayer.PositionInSongInMillis);
        }
        positionInSongInMillisOld = songAudioPlayer.PositionInSongInMillis;
    }

    private void StopThread()
    {
        thread.Stop();
        thread = null;

        midiManager.StopAllMidiNotes();
    }

    private void StartThread()
    {
        midiManager.InitIfNotDoneYet();
        thread = injector.CreateAndInject<SongEditorMidiSoundPlayAlongThread>();
        thread.Start((int)songAudioPlayer.PositionInSongInMillis);
    }
}
