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

public class ImportMidiFileButton : MonoBehaviour, INeedInjection
{
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    [Inject]
    private SongEditorMidiFileImporter midiFileImporter;

    void Start()
    {
        button.OnClickAsObservable()
            .Subscribe(_ => midiFileImporter.ImportMidiFile());
    }
}
