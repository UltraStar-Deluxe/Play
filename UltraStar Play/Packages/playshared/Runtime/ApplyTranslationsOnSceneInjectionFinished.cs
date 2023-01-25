using System.Diagnostics;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ApplyTranslationsOnSceneInjectionFinished : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    [Inject]
    private ISettings settings;

    [Inject]
    private TranslationManager translationManager;
    
    public void OnSceneInjectionFinished()
    {
        if (translationManager.currentLanguage != settings.Language)
        {
            translationManager.currentLanguage = settings.Language;
            translationManager.ReloadTranslationsAndUpdateScene();
        }

        if (Application.isPlaying)
        {
            // Fix: the method of the translationManager does not work, because scene.isLoaded is false for some reason.
            UpdateTranslatorsInScene();
        }
    }

    public void UpdateTranslatorsInScene()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        int count = 0;
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects)
        {
            ITranslator[] translators = rootObject.GetComponentsInChildren<ITranslator>();
            if (translators != null)
            {
                translators.ForEach(it =>
                {
                    it.UpdateTranslation();
                    count++;
                });
            }
        }

        if ((translationManager.logInfoInEditMode && !Application.isPlaying)
            || (translationManager.logInfoInPlayMode && Application.isPlaying))
        {
            Debug.Log($"Updated {count} ITranslator instances in scene took {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
