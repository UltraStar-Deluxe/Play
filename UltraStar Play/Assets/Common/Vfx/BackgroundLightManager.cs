using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UnityEngine.Serialization;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class BackgroundLightManager : AbstractSingletonBehaviour, INeedInjection
{
    public static BackgroundLightManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<BackgroundLightManager>();

    [InjectedInInspector]
    public RenderTexture backgroundLightRenderTexture;
        
    [InjectedInInspector]
    public GameObject backgroundLightInstancesParent;

    [Inject]
    private Settings settings;

    public int BackgroundLightInstancesCount => backgroundLightInstancesParent.transform.childCount;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        settings
            .ObserveEveryValueChanged(it => it.GraphicSettings.backgroundLightIndex)
            .Subscribe(newValue => SetActiveBackgroundLight(newValue));
    }

    private void SetActiveBackgroundLight(int index)
    {
        if (index <= 0)
        {
            RenderTextureUtils.Clear(backgroundLightRenderTexture);
            foreach (Transform child in backgroundLightInstancesParent.transform)
            {
                child.gameObject.SetActive(false);
            }
            return;
        }

        int iteration = 0;
        foreach (Transform child in backgroundLightInstancesParent.transform)
        {
            child.gameObject.SetActive(iteration == (index- 1));
            iteration++;
        }
    }
}
