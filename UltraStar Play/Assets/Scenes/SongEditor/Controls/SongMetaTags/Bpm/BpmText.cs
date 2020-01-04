using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

#pragma warning disable CS0649

public class BpmText : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    [Inject(searchMethod = SearchMethods.GetComponent)]
    private Text uiText;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    void Start()
    {
        SetBpm(songMeta.Bpm);
    }

    public void SetBpm(float bpm)
    {
        int bpmInt = (int)bpm;
        uiText.text = $"BPM: {bpmInt.ToString("F2", CultureInfo.InvariantCulture)}";
    }

    public void OnSceneInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(OnSongChanged);
    }

    private void OnSongChanged(ISongMetaChangeEvent changeEvent)
    {
        if (changeEvent is BpmChangeEvent || changeEvent is LoadedMementoEvent)
        {
            SetBpm(songMeta.Bpm);
        }
    }
}
