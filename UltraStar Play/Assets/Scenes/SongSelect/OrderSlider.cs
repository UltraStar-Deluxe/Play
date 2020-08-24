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

public class OrderSlider : TextItemSlider<ESongOrder>, INeedInjection
{
    [Inject]
    Settings settings;

    protected override void Start()
    {
        base.Start();
        Items = EnumUtils.GetValuesAsList<ESongOrder>();
        Selection.Value = settings.SongSelectSettings.songOrder;
        Selection.Subscribe(newSongOrder => settings.SongSelectSettings.songOrder = newSongOrder);
    }

    protected override string GetDisplayString(ESongOrder value)
    {
        return "Order: " + value;
    }
}
