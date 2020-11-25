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

public class AnalyzeBeatsWithoutTargetNoteSlider : BoolItemSlider, INeedInjection
{
    [Inject]
    private Settings settings;

    [Inject]
    private UiManager uiManager;

    protected override void Start()
    {
        base.Start();
        Selection.Value = settings.GraphicSettings.analyzeBeatsWithoutTargetNote;
        Selection.Subscribe(newValue =>
        {
            settings.GraphicSettings.analyzeBeatsWithoutTargetNote = newValue;
        });
    }
}
