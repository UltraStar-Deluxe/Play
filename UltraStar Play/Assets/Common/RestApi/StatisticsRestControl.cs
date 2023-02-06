using System.Net.Http;
using System.Text;
using SimpleHttpServerForUnity;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class StatisticsRestControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private HttpServer httpServer;

    [Inject]
    private Statistics statistics;

    private void Start()
    {
        httpServer.On(HttpMethod.Get, "api/rest/stats")
            .WithDescription($"Get statistics. This includes song scores, play count, etc.")
            .UntilDestroy(gameObject)
            .Do(requestData =>
            {
                string parameterValue = JsonConverter.ToJson(statistics);
                byte[] responseBytes = Encoding.UTF8.GetBytes(parameterValue);
                requestData.Context.Response.OutputStream.Write(responseBytes);
            });
	}
}
