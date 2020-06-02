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

public class NoteDisplayModeSlider : TextItemSlider<ENoteDisplayMode>, INeedInjection
{
    [Inject]
    private Settings settings;

    protected override void Start()
    {
        base.Start();
        Items = EnumUtils.GetValuesAsList<ENoteDisplayMode>();
        Selection.Value = settings.GraphicSettings.noteDisplayMode;
        Selection.Subscribe(newValue => settings.GraphicSettings.noteDisplayMode = newValue);
    }
}
