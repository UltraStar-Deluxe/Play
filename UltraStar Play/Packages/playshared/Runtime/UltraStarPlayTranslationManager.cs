using ProTrans;
using UniInject;
using UnityEngine;

public class UltraStarPlayTranslationManager : TranslationManager, INeedInjection
{
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
        if (sceneInjectionManager.SceneInjectionStatus == UltraStarPlaySceneInjectionManager.ESceneInjectionStatus.Pending)
        {
            sceneInjectionManager.DoSceneInjection();
        }
        base.UpdateTranslatorsInScene();
    }
}
