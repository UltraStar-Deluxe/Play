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

public class Notification : MonoBehaviour, INeedInjection
{
    [Inject]
    private UiManager uiManager;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    public RectTransform RectTransform { get; private set; }

    [InjectedInInspector]
    public Text uiText;

    [InjectedInInspector]
    public Image backgroundImage;

    void Start()
    {
        // After 2 seconds, fade away and destroy afterwards.
        LeanTween.alpha(backgroundImage.GetComponent<RectTransform>(), 0, 0.5f).setDelay(2f).setOnComplete(() => Destroy(gameObject));
        LeanTween.textAlpha(uiText.GetComponent<RectTransform>(), 0, 0.5f).setDelay(2f);
    }

    void OnDestroy()
    {
        if (uiManager != null)
        {
            uiManager.OnNotificationDestroyed(this);
        }
    }

    public void SetText(string message)
    {
        uiText.text = message;
    }
}
