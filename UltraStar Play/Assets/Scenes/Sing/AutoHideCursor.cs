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

/**
 * Hides the cursor when the mouse is not moved for a while.
 */
public class AutoHideCursor : MonoBehaviour
{
    private readonly float defaultHideDelayInSeconds = 5f;
    private float hideDelayInSeconds;

    private Vector3 lastMousePosition;

    private void Awake()
    {
        lastMousePosition = Input.mousePosition;
    }

    private void Update()
    {
        if (hideDelayInSeconds <= 0)
        {
            Cursor.visible = false;
        }
        else
        {
            hideDelayInSeconds -= Time.deltaTime;
        }
        if (lastMousePosition != Input.mousePosition
            || Input.anyKeyDown)
        {
            lastMousePosition = Input.mousePosition;

            Cursor.visible = true;
            hideDelayInSeconds = defaultHideDelayInSeconds;
        }
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
    }
}
