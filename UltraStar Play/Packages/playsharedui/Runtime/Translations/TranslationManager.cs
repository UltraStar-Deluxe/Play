using System;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

/**
 * This class is only used to store configuration values accessible via Unity Inspector.
 * To get translations, use static methods in Translaion class.
 */
public class TranslationManager : AbstractSingletonBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    public static TranslationManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<TranslationManager>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        Translation.InitTranslationConfig();
    }

    public bool generateConstantsOnResourceChange = true;

    [Inject]
    private UIDocument uiDocument;

    protected override object GetInstance()
    {
        return Instance;
    }

    public void OnSceneInjectionFinished()
    {
        ApplyTranslations();
    }

    public static void ApplyTranslations(VisualElement root = null)
    {
        TranslationManager translationManager = Instance;
        if (translationManager == null)
        {
            return;
        }
        translationManager.DoApplyTranslations(root);
    }

    private void DoApplyTranslations(VisualElement root = null)
    {
        root ??= uiDocument.rootVisualElement;

        // using DisposableStopwatch d = new($"Apply translations to '{root.name}' in frame {Time.frameCount}", ELogEventLevel.Verbose);

        root.Query<Label>().ForEach(label => ApplyTranslation(
            () => label.text,
            newValue => label.SetTranslatedText(newValue)));

        root.Query<Button>().ForEach(button => ApplyTranslation(
            () => button.text,
            newValue => button.SetTranslatedText(newValue)));

        root.Query<Chooser>().ForEach(chooser => ApplyTranslation(
            () => chooser.Label,
            newValue => chooser.SetTranslatedLabel(newValue)));

        root.Query<AccordionItem>().ForEach(accordionItem => ApplyTranslation(
            () => accordionItem.Title,
            newValue => accordionItem.SetTranslatedTitle(newValue)));
    }

    private static void ApplyTranslation(
        Func<string> textGetter,
        Action<Translation> textSetter)

    {
        string currentText = textGetter();
        if (!currentText.StartsWith(Translation.TranslationKeyPrefix))
        {
            return;
        }

        string translationKey = currentText.Substring(1).Trim();
        Translation translation = Translation.Get(translationKey);
        textSetter(translation);
    }
}
