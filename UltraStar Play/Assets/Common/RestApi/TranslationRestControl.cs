using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using ProTrans;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class TranslationRestControl : AbstractRestControl, INeedInjection
{
    public static TranslationRestControl Instance => DontDestroyOnLoadManager.FindComponentOrThrow<TranslationRestControl>();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        httpServer.CreateEndpoint(HttpMethod.Get, RestApiEndpointPaths.Language)
            .SetDescription($"Get current language as 2 letter country code")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                string twoLetterLanguageName = TranslationConfig.Singleton.CurrentCultureInfo.TwoLetterISOLanguageName;
                requestData.Context.Response.WriteJson(new Dictionary<string, string> { { "language", twoLetterLanguageName } });
            });

        httpServer.CreateEndpoint(HttpMethod.Get, RestApiEndpointPaths.Translations)
            .SetDescription($"Get all translations for the current language.")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                // Get fallback translations
                Dictionary<string, string> translations = new Dictionary<string, string>(Translation
                    .GetPropertiesFile(TranslationConfig.Singleton.CurrentCultureInfo)
                    .Dictionary);

                // Overwrite default translations for current language
                if (!Equals(TranslationConfig.Singleton.CurrentCultureInfo, new CultureInfo("en")))
                {
                    Translation.GetPropertiesFile(TranslationConfig.Singleton.CurrentCultureInfo)
                        .Dictionary.ForEach(entry => translations[entry.Key] = entry.Value);
                }

                requestData.Context.Response.WriteJson(translations);
            });
	}
}
