using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class BokehBackgroundManager : AbstractSingletonBehaviour, INeedInjection
{
    public static BokehBackgroundManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<BokehBackgroundManager>();
    
    [InjectedInInspector]
    public GameObject bokehBackgroundParent;

    [Inject]
    private Settings settings;

    public int BokehBackgroundCount => bokehBackgroundParent.transform.childCount;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        settings
            .ObserveEveryValueChanged(it => it.GraphicSettings.bokehBackgroundIndex)
            .Subscribe(newValue => SetBokehBackgroundActive(newValue));
    }

    private void SetBokehBackgroundActive(int index)
    {
        int iteration = 0;
        foreach (Transform child in bokehBackgroundParent.transform)
        {
            child.gameObject.SetActive(iteration == index);
            iteration++;
        }
    }
}
