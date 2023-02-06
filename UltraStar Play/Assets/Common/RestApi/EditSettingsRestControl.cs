using System.Net.Http;
using System.Text;
using SimpleHttpServerForUnity;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditSettingsRestControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private HttpServer httpServer;

    [Inject]
    private Settings settings;

    private void Start()
    {
        httpServer.On(HttpMethod.Get, "api/rest/config")
            .WithDescription($"Get config.")
            .UntilDestroy(gameObject)
            .Do(requestData =>
            {
                string parameterValue = JsonConverter.ToJson(settings);
                byte[] responseBytes = Encoding.UTF8.GetBytes(parameterValue);
                requestData.Context.Response.OutputStream.Write(responseBytes);
            });
        
        httpServer.On(HttpMethod.Post, "api/rest/config")
            .WithDescription($"Set config. Only present fields in the request body are set.")
            .UntilDestroy(gameObject)
            .Do(requestData =>
            {
                string jsonBody = requestData.Context.Request.GetBodyAsString();
                JsonConverter.FillFromJson(jsonBody, settings);
            });
	}
}
