using System.Collections.Generic;
using ProTrans;
using UniInject;
using UnityEngine;

public class UltraStarPlayTranslationManager : TranslationManager, INeedInjection
{
    public static UltraStarPlayTranslationManager Instance => TranslationManager.Instance as UltraStarPlayTranslationManager;
    
    [Inject]
    private Injector injector;

    public override void UpdateTranslatorsInScene()
    {
        if (!Application.isPlaying)
        {
            // Don't update in edit mode
            return;
        }

        UltraStarPlaySceneInjectionManager sceneInjectionManager = UltraStarPlaySceneInjectionManager.Instance;
        if (sceneInjectionManager.SceneInjectionStatus == ESceneInjectionStatus.Pending)
        {
            sceneInjectionManager.DoSceneInjection();
        }
        base.UpdateTranslatorsInScene();
    }

    public Dictionary<string, string> GetAllTranslations(bool includeFallbackTranslations)
    {
        Dictionary<string, string> allTranslations = new();
        if (includeFallbackTranslations)
        {
            fallbackMessages.ForEach(entry => allTranslations[entry.Key] = entry.Value);
        }
        currentLanguageMessages.ForEach(entry => allTranslations[entry.Key] = entry.Value);
        return allTranslations;
    }
}
