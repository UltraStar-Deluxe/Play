using System;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UniInject;
using UniRx;

public class SongEditorPositionHistoryNavigationControl : INeedInjection, IInjectionFinishedListener
{
    private const int MaxPositionHistoryLength = 50;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorSelectionControl songEditorSelectionControl;

    private readonly List<double> positionInMillisHistory = new();

    private int historyIndex;
    private double ignoreNewPositionInMillis = -1;

    public void OnInjectionFinished()
    {
        songAudioPlayer.PositionEventStream
            .Where(_ => !songAudioPlayer.IsPlaying)
            .Throttle(TimeSpan.FromMilliseconds(1000))
            .Subscribe(positionInMillis => AddNavigationPositionToHistory(positionInMillis));

        InputManager.GetInputAction(R.InputActions.songEditor_navigateForward).PerformedAsObservable()
            .Where(_ => !songAudioPlayer.IsPlaying && songEditorSelectionControl.IsSelectionEmpty)
            .Subscribe(_ => NavigateForwardInHistory());

        InputManager.GetInputAction(R.InputActions.songEditor_navigateBackward).PerformedAsObservable()
            .Where(_ => !songAudioPlayer.IsPlaying && songEditorSelectionControl.IsSelectionEmpty)
            .Subscribe(_ => NavigateBackwardInHistory());

        AddInitialNavigationPositionToHistory();
    }

    private void AddInitialNavigationPositionToHistory()
    {
        // Short delay because initial position may not be set yet.
        MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(0.1f,
            () => AddNavigationPositionToHistory(songAudioPlayer.PositionInMillis)));
    }

    private void NavigateBackwardInHistory()
    {
        int nextHistoryIndex = historyIndex + 1;
        int indexInHistoryArray = positionInMillisHistory.Count - nextHistoryIndex - 1;
        if (indexInHistoryArray < 0
            || indexInHistoryArray >= positionInMillisHistory.Count)
        {
            return;
        }
        historyIndex = nextHistoryIndex;

        double loadedHistoryPositionInMillis = positionInMillisHistory[indexInHistoryArray];
        ignoreNewPositionInMillis = loadedHistoryPositionInMillis;
        songAudioPlayer.PositionInMillis = loadedHistoryPositionInMillis;
    }

    private void NavigateForwardInHistory()
    {
        int nextHistoryIndex = historyIndex - 1;
        int indexInHistoryArray = positionInMillisHistory.Count - nextHistoryIndex - 1;
        if (indexInHistoryArray < 0
            || indexInHistoryArray >= positionInMillisHistory.Count)
        {
            return;
        }
        historyIndex = nextHistoryIndex;

        double loadedHistoryPositionInMillis = positionInMillisHistory[indexInHistoryArray];
        ignoreNewPositionInMillis = loadedHistoryPositionInMillis;
        songAudioPlayer.PositionInMillis = loadedHistoryPositionInMillis;
    }

    private void AddNavigationPositionToHistory(double positionInMillis)
    {
        if (ignoreNewPositionInMillis >= 0
            && Math.Abs(ignoreNewPositionInMillis - positionInMillis) < 1)
        {
            ignoreNewPositionInMillis = -1;
            return;
        }

        // Remove discarded positions from history
        while (historyIndex > 0
               && positionInMillisHistory.Count > 0)
        {
            positionInMillisHistory.RemoveLast();
            historyIndex--;
        }

        if (positionInMillisHistory.Count >= MaxPositionHistoryLength)
        {
            positionInMillisHistory.RemoveLast();
        }

        if (positionInMillisHistory.Count > 0
            && Math.Abs(positionInMillisHistory.LastOrDefault() - positionInMillis) < 1000)
        {
            // Ignore similar position
            return;
        }

        positionInMillisHistory.Add(positionInMillis);
    }
}
