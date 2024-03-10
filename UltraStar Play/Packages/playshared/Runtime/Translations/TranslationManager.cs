using ProTrans;
using UniInject;

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
