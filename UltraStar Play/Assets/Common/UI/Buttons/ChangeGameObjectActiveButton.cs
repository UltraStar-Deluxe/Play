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
public class ChangeGameObjectActiveButton : MonoBehaviour, INeedInjection
{
    public enum EChangeMode
    {
        SetActive,
        SetInactive,
        Toggle
    }

    public GameObject target;
    public EChangeMode changeMode;

    void Start()
    {
        if (target == null)
        {
            throw new UnityException("No target set to change active state");
        }

        GetComponent<Button>().OnClickAsObservable()
            .Subscribe(_ =>
            {
                switch (changeMode)
                {
                    case EChangeMode.SetActive:
                        target.SetActive(true);
                        break;
                    case EChangeMode.SetInactive:
                        target.SetActive(false);
                        break;
                    case EChangeMode.Toggle:
                        target.SetActive(!target.activeSelf);
                        break;
                    default:
                        throw new UnityException("Unkown change mode: " + changeMode);
                }
            });
    }
}
