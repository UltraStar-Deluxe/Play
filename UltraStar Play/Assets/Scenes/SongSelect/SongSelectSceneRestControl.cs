using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectSceneRestControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongRouletteController songRouletteController;
    
	private void Start() {
        HttpServer.Instance.AddRouteHandler(HttpMethod.Post, "api/rest/selectNextSong", _ =>
        {
            songRouletteController.SelectNextSong();
            return "";
        });
	}
}
