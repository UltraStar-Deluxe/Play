using UnityEngine;
using UniInject;
using UnityEngine.UIElements;
using UniRx;
using System;
using System.Collections.Generic;

public class MicRecordingSaverLifeCycle : IOnLoadMod, IOnDisableMod
{
    public void OnLoadMod()
    {
        Debug.Log($"{nameof(MicRecordingSaverLifeCycle)}.OnLoadMod");
    }

    public void OnDisableMod()
    {
        Debug.Log($"{nameof(MicRecordingSaverLifeCycle)}.OnDisableMod");
    }
}
