using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using SimpleHttpServerForUnity;
using UnityEngine;
using UniInject;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditSettingsRestControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private HttpServer httpServer;

    [Inject]
    private Settings settings;

    private Dictionary<string, ConfigGetterAndSetter> configGettersAndSetters;

    private void Start()
    {
        configGettersAndSetters = CreateConfigGettersAndSetters();

        httpServer.On(HttpMethod.Get, "api/rest/config/{parameterName}")
            .WithDescription($"Get config value")
            .UntilDestroy(gameObject)
            .Do(requestData =>
            {
                string parameterName = requestData.PathParameters["parameterName"];
                string parameterValue = GetConfigParameter(parameterName);
                byte[] responseBytes = Encoding.UTF8.GetBytes(parameterValue);
                requestData.Context.Response.OutputStream.Write(responseBytes);
            });

        httpServer.On(HttpMethod.Post, "api/rest/config/{parameterName}/{newValue}")
            .WithDescription($"Set config value")
            .UntilDestroy(gameObject)
            .Do(requestData =>
            {
                string parameterName = requestData.PathParameters["parameterName"];
                string newValue = requestData.PathParameters["newValue"];
                SetConfigParameter(parameterName, newValue);
            });
	}

    private string GetConfigParameter(string parameterName)
    {
        if (!configGettersAndSetters.TryGetValue(parameterName, out ConfigGetterAndSetter configGetterAndSetter))
        {
            throw new UltraStarPlayException($"Unhandled config parameter '{parameterName}'");
        }

        return configGetterAndSetter.getter();
    }

    private void SetConfigParameter(string parameterName, string newValue)
    {
        if (!configGettersAndSetters.TryGetValue(parameterName, out ConfigGetterAndSetter configGetterAndSetter))
        {
            throw new UltraStarPlayException($"Unhandled config parameter '{parameterName}'");
        }

        configGetterAndSetter.setter(newValue);
    }

    private Dictionary<string, ConfigGetterAndSetter> CreateConfigGettersAndSetters()
    {
        Dictionary<string, ConfigGetterAndSetter> result = new();
        void AddConfigGetterAndSetter(ConfigGetterAndSetter configGetterAndSetter)
        {
            result.Add(configGetterAndSetter.propertyName, configGetterAndSetter);
        }

        AddConfigGetterAndSetter(new ConfigGetterAndSetter("volume",
            () => settings.AudioSettings.VolumePercent,
            newValue => PropertyUtils.TrySetIntFromString(newValue, newIntValue => settings.AudioSettings.VolumePercent = newIntValue)));
        return result;
    }

    private class ConfigGetterAndSetter
    {
        public string propertyName;
        public Func<string> getter;
        public Action<string> setter;

        public ConfigGetterAndSetter(string propertyName, Func<object> getter, Action<string> setter)
        {
            this.propertyName = propertyName;
            this.getter = () =>
            {
                object currentValue = getter();
                return currentValue == null
                    ? ""
                    : getter().ToString();
            };
            this.setter = setter;
        }
    }
}
