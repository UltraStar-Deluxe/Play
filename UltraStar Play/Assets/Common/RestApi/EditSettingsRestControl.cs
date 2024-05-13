using System.Net.Http;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditSettingsRestControl : AbstractRestControl, INeedInjection
{
    public static EditSettingsRestControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<EditSettingsRestControl>();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        httpServer.CreateEndpoint(HttpMethod.Get, HttpApiEndpointPaths.Config)
            .SetDescription($"Get config.")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                requestData.Context.Response.WriteJson(settings);
            });

        httpServer.CreateEndpoint(HttpMethod.Post, HttpApiEndpointPaths.Config)
            .SetDescription($"Set config. Only present fields in the request body are set.")
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(HttpApiPermission.WriteConfig)
            .SetCallbackAndAdd(requestData =>
            {
                string jsonBody = requestData.Context.Request.GetBodyAsString();
                JsonConverter.FillFromJson(jsonBody, settings);
            });
	}
}
