using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using System.Linq;

public class AmplificationSlider : TextItemSlider<int>
{
    private IDisposable disposable;

    protected override void Awake()
    {
        base.Awake();
        Items = new List<int> { 0, 6, 12, 18 };
    }

    public void SetMicProfile(MicProfile micProfile)
    {
        if (disposable != null)
        {
            disposable.Dispose();
        }

        Selection.Value = Items.Where(it => it == micProfile.Amplification).First();
        disposable = Selection.Subscribe(newValue => micProfile.Amplification = newValue);
    }

    protected override string GetDisplayString(int value)
    {
        return $"{value} dB";
    }

}
