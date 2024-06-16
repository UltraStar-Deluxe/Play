using System;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneMedleyControl : INeedInjection, IInjectionFinishedListener
{
    private const int CountDownTimeInSeconds = 5;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private Settings settings;

    [Inject]
    private SingSceneCountdownControl countdownControl;

    [Inject]
    private SingSceneAudioFadeInControl audioFadeInControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private SingSceneData sceneData;

    [Inject]
    private SingSceneFinisher singSceneFinisher;

    public int MedleyStartBeat {get; private set;}
    public int MedleyEndBeat {get; private set;}
    public double MedleyStartWithCountdownInMillis {get; private set;}
    public double MedleyStartWithoutCountdownInMillis {get; private set;}
    public double MedleyEndInMillis {get; private set;}
    public double MedleyDurationWithCountdownInMillis {get; private set;}

    private bool IsMedley => sceneData.IsMedley;

    public void OnInjectionFinished()
    {
        MedleyStartBeat = CalculateMedleyStartBeat();
        MedleyEndBeat = CalculateMedleyEndBeat();
        MedleyStartWithoutCountdownInMillis = CalculateMedleyStartWithoutCountdownInMillis();
        MedleyStartWithCountdownInMillis = CalculateMedleyStartWithCountdownInMillis();
        MedleyEndInMillis = CalculateMedleyEndInMillis();
        MedleyDurationWithCountdownInMillis = CalculateMedleyDurationWithCountdownInMillis();
    }

    public void Update()
    {
        if (!IsMedley)
        {
            return;
        }

        if (!singSceneFinisher.IsSongFinished
            && Math.Abs(songAudioPlayer.PositionInMillis - CalculateMedleyEndInMillis()) < 1000)
        {
            Debug.Log($"Trigger medley song finish");
            singSceneFinisher.TriggerEarlySongFinish();
        }
    }

    public double CurrentTimeInSongInPercentConsideringMedley
    {
        get
        {
            if (!IsMedley)
            {
                return songAudioPlayer.PositionInPercent;
            }
            return (songAudioPlayer.PositionInMillis - MedleyStartWithCountdownInMillis)
                            / MedleyDurationWithCountdownInMillis;
        }
    }

    public void StartCurrentMedleySong()
    {
        if (!IsMedley)
        {
            return;
        }

        if (!songAudioPlayer.IsFullyLoaded)
        {
            IDisposable songAudioLoadedDisposable = null;
            songAudioLoadedDisposable = songAudioPlayer.LoadedEventStream.Subscribe(_ =>
            {
                DoStartCurrentMedleySong();
                songAudioLoadedDisposable?.Dispose();
            });
            return;
        }

        DoStartCurrentMedleySong();
    }

    private void DoStartCurrentMedleySong()
    {
        Debug.Log($"Starting current medley song '{singSceneControl.SongMeta.GetArtistDashTitle()}'");
        singSceneControl.SkipToPosition(CalculateMedleyStartWithCountdownInMillis());
        countdownControl.StartCountdown(CountDownTimeInSeconds);
        audioFadeInControl.StartAudioFadeIn(CountDownTimeInSeconds);
    }

    private double CalculateMedleyStartWithCountdownInMillis()
    {
        if (!IsMedley)
        {
            return 0;
        }
        return NumberUtils.Limit(CalculateMedleyStartWithoutCountdownInMillis() - CountDownTimeInSeconds * 1000, 0, double.MaxValue);
    }

    private double CalculateMedleyStartWithoutCountdownInMillis()
    {
        if (!IsMedley)
        {
            return 0;
        }
        SongMeta songMeta = singSceneControl.SongMeta;
        int medleyStartBeat = SongMetaUtils.GetMedleyStartBeat(songMeta);
        return SongMetaBpmUtils.BeatsToMillis(songMeta, medleyStartBeat);
    }

    private double CalculateMedleyEndInMillis()
    {
        if (!IsMedley)
        {
            return songAudioPlayer.DurationInMillis;
        }
        SongMeta songMeta = singSceneControl.SongMeta;
        int medleyEndBeat = SongMetaUtils.GetMedleyEndBeat(songMeta, settings.DefaultMedleyTargetDurationInSeconds);
        return SongMetaBpmUtils.BeatsToMillis(songMeta, medleyEndBeat);
    }

    private double CalculateMedleyDurationWithCountdownInMillis()
    {
        return CalculateMedleyEndInMillis() - CalculateMedleyStartWithCountdownInMillis();
    }


    private int CalculateMedleyStartBeat()
    {
        if (!IsMedley)
        {
            return 0;
        }

        return SongMetaUtils.GetMedleyStartBeat(singSceneControl.SongMeta);
    }

    private int CalculateMedleyEndBeat()
    {
        if (!IsMedley)
        {
            return SongMetaUtils.MaxBeat(SongMetaUtils.GetAllNotes(singSceneControl.SongMeta));
        }

        return SongMetaUtils.GetMedleyEndBeat(singSceneControl.SongMeta, settings.DefaultMedleyTargetDurationInSeconds);
    }

    public bool IsNoteInMedleyRange(Note note)
    {
        return IsBeatInMedleyRange(note.StartBeat) && IsBeatInMedleyRange(note.EndBeat);
    }

    public bool IsSentenceInMedleyRange(Sentence sentence)
    {
        return IsBeatInMedleyRange(sentence.MinBeat) && IsBeatInMedleyRange(sentence.MaxBeat);
    }

    public bool IsBeatInMedleyRange(int beat)
    {
        if (!IsMedley)
        {
            return true;
        }
        return MedleyStartBeat <= beat && beat <= MedleyEndBeat;
    }
}
