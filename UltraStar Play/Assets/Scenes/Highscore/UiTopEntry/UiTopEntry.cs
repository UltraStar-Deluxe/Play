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

public class UiTopEntry : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public Text indexText;
    [InjectedInInspector]
    public Text playerNameText;
    [InjectedInInspector]
    public Text scoreText;
    [InjectedInInspector]
    public Text dateText;
}
