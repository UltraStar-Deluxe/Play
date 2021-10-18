using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ApplyBpmDontAdjustNoteLengthButton : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public InputField newBpmInputField;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private ApplyBpmDontAdjustNoteLengthAction applyBpmDontAdjustNoteLengthAction;

    void Start()
    {
        button.OnClickAsObservable().Subscribe(_ =>
        {
            if (float.TryParse(newBpmInputField.text, out float newBpm))
            {
                applyBpmDontAdjustNoteLengthAction.ExecuteAndNotify(newBpm);
            }
        });
    }
}
