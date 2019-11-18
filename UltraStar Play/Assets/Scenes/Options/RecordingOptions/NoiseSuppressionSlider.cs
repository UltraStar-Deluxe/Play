using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

public class NoiseSuppressionSlider : TextItemSlider<int>
{
    private IDisposable disposable;

    protected override void Awake()
    {
        base.Awake();
        Items = new List<int> { 0, 5, 10, 15, 20, 25, 30 };
    }

    public void SetMicProfile(MicProfile micProfile)
    {
        if (disposable != null)
        {
            disposable.Dispose();
        }

        Selection.Value = Items.Where(it => it == micProfile.NoiseSuppression).First();
        disposable = Selection.Subscribe(newValue => micProfile.NoiseSuppression = newValue);
    }

    protected override string GetDisplayString(int value)
    {
        return $"{value} %";
    }
}
