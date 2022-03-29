using System.Net.Http;
using SimpleHttpServerForUnity;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectSceneRestControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongRouletteControl songRouletteControl;
    
	private void Start() {
        HttpServer.Instance.On(HttpMethod.Post, "api/rest/selectNextSong")
            .WithDescription("Select the next song")
            .UntilDestroy(gameObject)
            .Do(_ => songRouletteControl.SelectNextSong());
	}
}
