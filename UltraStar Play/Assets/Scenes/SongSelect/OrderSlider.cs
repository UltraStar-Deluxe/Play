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

public class OrderSlider : TextItemSlider<ESongSelectOrder>, INeedInjection
{
    protected override void Start()
    {
        base.Start();
        Items = EnumUtils.GetValuesAsList<ESongSelectOrder>();
        Selection.Value = Items[0];
    }

    protected override string GetDisplayString(ESongSelectOrder value)
    {
        return "Order: " + value;
    }
}
