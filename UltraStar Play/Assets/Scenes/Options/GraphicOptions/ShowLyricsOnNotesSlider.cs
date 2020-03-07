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

public class ShowLyricsOnNotesSlider : BoolItemSlider
{
    protected override void Start()
    {
        base.Start();
        Selection.Value = SettingsManager.Instance.Settings.GraphicSettings.showLyricsOnNotes;
        Selection.Subscribe(newValue => SettingsManager.Instance.Settings.GraphicSettings.showLyricsOnNotes = newValue);
    }
}
