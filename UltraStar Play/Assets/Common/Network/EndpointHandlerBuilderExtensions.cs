using System;
using SimpleHttpServerForUnity;

public static class EndpointHandlerBuilderExtensions
{
    public static void SetCallbackAndAdd(this EndpointHandlerBuilder endpointHandlerBuilder, Action<EndpointRequestData> requestCallback)
    {
        endpointHandlerBuilder.SetCallback(requestCallback);
        endpointHandlerBuilder.Add();
    }
}
