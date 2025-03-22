using System;
using System.Collections.Generic;
using System.Net;
using SimpleHttpServerForUnity;

public static class EndpointHandlerBuilderExtensions
{
    public static void SetCallbackAndAdd(this EndpointHandlerBuilder endpointHandlerBuilder, Action<EndpointRequestData> requestCallback)
    {
        endpointHandlerBuilder.SetCallback(requestCallback);
        endpointHandlerBuilder.Add();
    }

    public static EndpointHandlerBuilder SetRequiredPermission(this EndpointHandlerBuilder endpointHandlerBuilder, RestApiPermission requiredPermission, Settings settings)
    {
        if (!settings.RequireCompanionClientPermission)
        {
            // Permissions are disabled
            return endpointHandlerBuilder;
        }

        endpointHandlerBuilder.SetCondition(requestData =>
        {
            string clientId = requestData.Context.Request.Headers["client-id"];
            Settings settings = SettingsManager.Instance.Settings;

            List<RestApiPermission> permissions = SettingsUtils.GetPermissions(settings, clientId);
            if (permissions.Contains(requiredPermission))
            {
                return true;
            }

            requestData.Context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            requestData.Context.Response.ContentType = MimeTypeUtils.ApplicationJson;
            requestData.Context.Response.WriteJson(new ErrorMessageDto($"Missing permission {requiredPermission}"));
            return false;
        });
        return endpointHandlerBuilder;
    }

    public static EndpointHandlerBuilder AddUserData(this EndpointHandlerBuilder endpointHandlerBuilder, object key, object value)
    {
        endpointHandlerBuilder.UserData[key] = value;
        return endpointHandlerBuilder;
    }
}
