using UniInject;

/**
 * This class is only used to store configuration values accessible via Unity Inspector.
 * To get translations, use static methods in Translaion class.
 */
public class TranslationManager : AbstractSingletonBehaviour, INeedInjection
{
    public static TranslationManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<TranslationManager>();

    public bool generateConstantsOnResourceChange = true;

    protected override object GetInstance()
    {
        return Instance;
    }

    public static void ReloadTranslationsAndUpdateScene()
    {
    }
}
