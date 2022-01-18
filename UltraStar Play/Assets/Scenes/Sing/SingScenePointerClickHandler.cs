using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingScenePointerClickHandler : MonoBehaviour, INeedInjection, IPointerClickHandler
{
    [Inject]
    private SingSceneControl singSceneControl;
    
    private float lastClickTime;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        bool isDoubleClick = Time.time - lastClickTime < InputUtils.DoubleClickThresholdInSeconds;
        lastClickTime = Time.time;
        if (isDoubleClick)
        {
            singSceneControl.TogglePlayPause();
        }
    }
}
