using UnityEngine;
using UniInject;
using UniRx;

#pragma warning disable CS0649

public class NoteAreaPositionInSongIndicator : MonoBehaviour, INeedInjection
{

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject(searchMethod = SearchMethods.GetComponentInParent)]
    private NoteArea noteArea;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    void Start()
    {
        songAudioPlayer.PositionInSongEventStream.Subscribe(SetPositionInSongInMillis);
        noteArea.ViewportEventStream.Subscribe(_ => SetPositionInSongInMillis(songAudioPlayer.PositionInSongInMillis));
    }

    private void SetPositionInSongInMillis(double positionInSongInMillis)
    {
        float x = (float)noteArea.GetHorizontalPositionForMillis(positionInSongInMillis);
        rectTransform.anchorMin = new Vector2(x, 0);
        rectTransform.anchorMax = new Vector2(x, 1);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 0);
    }

}