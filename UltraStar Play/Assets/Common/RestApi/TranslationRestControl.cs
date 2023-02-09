using System.Collections.Generic;
using System.Net.Http;
using ProTrans;
using SimpleHttpServerForUnity;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class TranslationRestControl : AbstractRestControl, INeedInjection
{
    public static TranslationRestControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<TranslationRestControl>();
    
    [Inject]
    private UltraStarPlayTranslationManager translationManager;

    protected override object GetInstance()
    {
        return Instance;
    }
    
    protected override void StartSingleton()
    {
        httpServer.CreateEndpoint(HttpMethod.Get, "api/rest/language")
            .SetDescription($"Get current language as 2 letter country code")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                string language = LanguageHelper.Get2LetterIsoCodeFromSystemLanguage(translationManager.currentLanguage);
                requestData.Context.Response.WriteJson(new Dictionary<string, string> { { "language", language } });
            });

        httpServer.CreateEndpoint(HttpMethod.Get, "api/rest/translations")
            .SetDescription($"Get all translations for the current language.")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                requestData.Context.Response.WriteJson(translationManager.GetAllTranslations(true));
            });
	}
}
