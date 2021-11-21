using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class RecordingDevicePickerControl : LabeledItemPickerControl<MicProfile>
{
    public RecordingDevicePickerControl(ItemPicker itemPicker, List<MicProfile> items)
        : base(itemPicker, items)
    {
    }

    protected override string GetLabelText(MicProfile micProfile)
    {
        if (micProfile == null)
        {
            return "";
        }
        else
        {
            return micProfile.Name;
        }
    }
}
