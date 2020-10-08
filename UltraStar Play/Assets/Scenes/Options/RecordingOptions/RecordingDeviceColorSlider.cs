using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class RecordingDeviceColorSlider : ColorItemSlider
{
    private IDisposable disposable;

    protected override void Awake()
    {
        base.Awake();
        Items = new List<Color>(ColorPalettes.Aussie.colors);
    }

    public void SetMicProfile(MicProfile micProfile)
    {
        if (disposable != null)
        {
            disposable.Dispose();
        }

        Color matchingColor = Items.Where(it => it == micProfile.Color).FirstOrDefault();
        if (matchingColor == null)
        {
            matchingColor = Items[0];
            micProfile.Color = Items[0];
        }
        Selection.Value = matchingColor;
        disposable = Selection.Subscribe(newValue => micProfile.Color = newValue);
    }
}
