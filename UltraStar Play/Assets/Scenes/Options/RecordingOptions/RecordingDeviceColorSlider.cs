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
        Items = new List<Color> { Colors.Crimson, Colors.ForestGreen, Colors.DodgerBlue,
                                  Colors.Gold, Colors.GreenYellow, Colors.Salmon, Colors.Violet };
    }

    public void SetMicProfile(MicProfile micProfile)
    {
        if (disposable != null)
        {
            disposable.Dispose();
        }

        Selection.Value = Items.Where(it => it == micProfile.Color).First();
        disposable = Selection.Subscribe(newValue => micProfile.Color = newValue);
    }
}
