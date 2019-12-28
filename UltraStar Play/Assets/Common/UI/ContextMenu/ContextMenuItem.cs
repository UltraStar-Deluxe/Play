using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

[RequireComponent(typeof(Button))]
public class ContextMenuItem : MonoBehaviour
{
    private IDisposable actionSubscriptionDisposable;

    private Text uiText;
    private Button button;

    public string Text
    {
        get
        {
            return uiText.text;
        }
        set
        {
            uiText.text = value;
        }
    }

    void Awake()
    {
        uiText = GetComponentInChildren<Text>();
        button = GetComponent<Button>();
    }

    public void SetAction(Action action)
    {
        if (actionSubscriptionDisposable != null)
        {
            actionSubscriptionDisposable.Dispose();
            actionSubscriptionDisposable = null;
        }
        if (action != null)
        {
            actionSubscriptionDisposable = button.OnClickAsObservable().Subscribe(_ => action());
        }
    }
}