using System.Net.Http;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class StatisticsRestControl : AbstractRestControl, INeedInjection
{
    public static StatisticsRestControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<StatisticsRestControl>();

    [Inject]
    private Statistics statistics;

    protected override object GetInstance()
    {
        return Instance;
    }
    
    protected override void StartSingleton()
    {
        httpServer.CreateEndpoint(HttpMethod.Get, HttpApiEndpointPaths.Statistics)
            .SetDescription($"Get statistics. This includes song scores, play count, etc.")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                requestData.Context.Response.WriteJson(statistics);
            });
	}
}
