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

[RequireComponent(typeof(Button))]
public class ChangeSceneButton : MonoBehaviour, INeedInjection
{
    public EScene targetScene;

    void Start()
    {
        GetComponent<Button>().OnClickAsObservable()
            .Subscribe(_ => SceneNavigator.Instance.LoadScene(targetScene));
    }
}
