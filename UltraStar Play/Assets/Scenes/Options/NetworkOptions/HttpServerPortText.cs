using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using SimpleHttpServerForUnity;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class HttpServerPortText : MonoBehaviour, INeedInjection
{
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;
    
	private void Start()
    {
        if (HttpServer.IsSupported)
        {
            uiText.text = $"Http Server Port: {HttpServer.Instance.port}\n"
                + $"Example: http://{HttpServer.Instance.host}:{HttpServer.Instance.port}/api/rest/songs";    
        }
        else
        {
            uiText.text = "Http server not supported on this platform.";
        }
    }
}
