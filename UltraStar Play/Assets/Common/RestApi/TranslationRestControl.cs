using System.Net.Http;
using System.Text;
using ProTrans;
using SimpleHttpServerForUnity;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class TranslationRestControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private HttpServer httpServer;

    [Inject]
    private UltraStarPlayTranslationManager translationManager;

    private void Start()
    {
        httpServer.On(HttpMethod.Get, "api/rest/language")
            .WithDescription($"Get current language as 2 letter country code")
            .UntilDestroy(gameObject)
            .Do(requestData =>
            {
                string parameterValue = LanguageHelper.Get2LetterIsoCodeFromSystemLanguage(translationManager.currentLanguage);
                byte[] responseBytes = Encoding.UTF8.GetBytes(parameterValue);
                requestData.Context.Response.OutputStream.Write(responseBytes);
            });

        httpServer.On(HttpMethod.Get, "api/rest/translations")
            .WithDescription($"Get all translations for the current language.")
            .UntilDestroy(gameObject)
            .Do(requestData =>
            {
                string parameterValue = JsonConverter.ToJson(translationManager.GetAllTranslations(true));
                byte[] responseBytes = Encoding.UTF8.GetBytes(parameterValue);
                requestData.Context.Response.OutputStream.Write(responseBytes);
            });
	}
}
