using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

public class BpmText : MonoBehaviour, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponent)]
    private Text uiText;

    [Inject]
    private SongMeta songMeta;

    void Start()
    {
        SetBpm(songMeta.Bpm);
    }

    public void SetBpm(float bpm)
    {
        int bpmInt = (int)bpm;
        uiText.text = bpmInt + " BPM\n";
    }
}
