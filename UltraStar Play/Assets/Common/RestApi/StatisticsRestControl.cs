using System.Net.Http;
using System.Text;
using SimpleHttpServerForUnity;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class StatisticsRestControl : AbstractSingletonBehaviour, INeedInjection
{
    public static StatisticsRestControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<StatisticsRestControl>();

    [Inject]
    private HttpServer httpServer;

    [Inject]
    private Statistics statistics;

    protected override object GetInstance()
    {
        return Instance;
    }
    
    protected override void StartSingleton()
    {
        httpServer.CreateEndpoint(HttpMethod.Get, "api/rest/stats")
            .SetDescription($"Get statistics. This includes song scores, play count, etc.")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                requestData.Context.Response.WriteJson(statistics);
            });
	}
}
