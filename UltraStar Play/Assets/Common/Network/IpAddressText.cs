using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleHttpServerForUnity;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class IpAddressText : MonoBehaviour, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;
    
	private void Start()
    {
        uiText.text = "IP: " + HttpServer.Instance.host;
    }
}
