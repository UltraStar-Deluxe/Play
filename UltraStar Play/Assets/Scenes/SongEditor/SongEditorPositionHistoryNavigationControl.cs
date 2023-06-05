using System;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;

public class SongEditorPositionHistoryNavigationControl : INeedInjection, IInjectionFinishedListener
{
    private const int MaxPositionHistoryLength = 50;
    
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    private readonly List<double> positionInSongInMillisHistory = new();

    private int historyIndex;
    private double ignoreNewPositionInSongInMillis = -1;
    
    public void OnInjectionFinished()
    {
        songAudioPlayer.PositionInSongEventStream
            .Where(_ => !songAudioPlayer.IsPlaying)
            .Throttle(TimeSpan.FromMilliseconds(1000))
            .Subscribe(positionInSongInMillis => AddNavigationPositionToHistory(positionInSongInMillis));

        InputManager.GetInputAction(R.InputActions.songEditor_navigateForward).PerformedAsObservable()
            .Where(_ => !songAudioPlayer.IsPlaying)
            .Subscribe(_ => NavigateForwardInHistory());
        
        InputManager.GetInputAction(R.InputActions.songEditor_navigateBackward).PerformedAsObservable()
            .Where(_ => !songAudioPlayer.IsPlaying)
            .Subscribe(_ => NavigateBackwardInHistory());
        
        AddInitialNavigationPositionToHistory();
    }

    private void AddInitialNavigationPositionToHistory()
    {
        // Short delay because initial position may not be set yet.
        MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(0.1f,
            () => AddNavigationPositionToHistory(songAudioPlayer.PositionInSongInMillis)));
    }

    private void NavigateBackwardInHistory()
    {
        int nextHistoryIndex = historyIndex + 1;
        int indexInHistoryArray = positionInSongInMillisHistory.Count - nextHistoryIndex - 1;
        if (indexInHistoryArray < 0
            || indexInHistoryArray >= positionInSongInMillisHistory.Count)
        {
            return;
        }
        historyIndex = nextHistoryIndex;

        double loadedHistoryPositionInMillis = positionInSongInMillisHistory[indexInHistoryArray];
        ignoreNewPositionInSongInMillis = loadedHistoryPositionInMillis;
        songAudioPlayer.PositionInSongInMillis = loadedHistoryPositionInMillis;
    }

    private void NavigateForwardInHistory()
    {
        int nextHistoryIndex = historyIndex - 1;
        int indexInHistoryArray = positionInSongInMillisHistory.Count - nextHistoryIndex - 1;
        if (indexInHistoryArray < 0
            || indexInHistoryArray >= positionInSongInMillisHistory.Count)
        {
            return;
        }
        historyIndex = nextHistoryIndex;
        
        double loadedHistoryPositionInMillis = positionInSongInMillisHistory[indexInHistoryArray];
        ignoreNewPositionInSongInMillis = loadedHistoryPositionInMillis;
        songAudioPlayer.PositionInSongInMillis = loadedHistoryPositionInMillis;
    }

    private void AddNavigationPositionToHistory(double positionInSongInMillis)
    {
        if (ignoreNewPositionInSongInMillis >= 0 
            && Math.Abs(ignoreNewPositionInSongInMillis - positionInSongInMillis) < 1)
        {
            ignoreNewPositionInSongInMillis = -1;
            return;
        }
        
        // Remove discarded positions from history
        while (historyIndex > 0
               && positionInSongInMillisHistory.Count > 0)
        {
            positionInSongInMillisHistory.RemoveLast();
            historyIndex--;
        }
        
        if (positionInSongInMillisHistory.Count >= MaxPositionHistoryLength)
        {
            positionInSongInMillisHistory.RemoveLast();
        }
        
        if (positionInSongInMillisHistory.Count > 0
            && Math.Abs(positionInSongInMillisHistory.LastOrDefault() - positionInSongInMillis) < 1000)
        {
            // Ignore similar position
            return;
        }
        
        positionInSongInMillisHistory.Add(positionInSongInMillis);
    }
}
