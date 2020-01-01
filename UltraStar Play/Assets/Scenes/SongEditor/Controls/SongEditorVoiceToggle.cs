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

public class SongEditorVoiceToggle : MonoBehaviour, INeedInjection
{
    public int voiceIndex;

    private string voiceName;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Toggle toggle;

    [Inject]
    private Settings settings;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    void Start()
    {
        voiceName = "P" + (voiceIndex + 1);

        bool isHidden = settings.SongEditorSettings.HideVoices.Contains(voiceName);
        toggle.isOn = !isHidden;
        toggle.OnValueChangedAsObservable().Subscribe(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isVisible)
    {
        if (isVisible)
        {
            settings.SongEditorSettings.HideVoices.Remove(voiceName);
        }
        else
        {
            settings.SongEditorSettings.HideVoices.AddIfNotContains(voiceName);
        }
        editorNoteDisplayer.UpdateNotesAndSentences();
    }
}
