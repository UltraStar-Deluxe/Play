using UnityEngine;
using UniInject;
using UnityEngine.UIElements;
using UniRx;
using System;
using System.Collections.Generic;

public class MicInputSaverLifeCycle : IOnLoadMod, IOnDisableMod
{
    public void OnLoadMod()
    {
        Debug.Log($"{nameof(MicInputSaverLifeCycle)}.OnLoadMod");
    }

    public void OnDisableMod()
    {
        Debug.Log($"{nameof(MicInputSaverLifeCycle)}.OnDisableMod");
    }
}
