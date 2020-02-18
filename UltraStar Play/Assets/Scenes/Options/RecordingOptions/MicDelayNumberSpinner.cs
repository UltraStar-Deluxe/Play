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

public class MicDelayNumberSpinner : NumberSpinner, INeedInjection
{
    private IDisposable disposable;

    public void SetMicProfile(MicProfile micProfile)
    {
        if (disposable != null)
        {
            disposable.Dispose();
        }

        SelectedValue = micProfile.DelayInMillis;
        disposable = SelectedValueStream.Subscribe(newValue => micProfile.DelayInMillis = (int)newValue);
    }
}
