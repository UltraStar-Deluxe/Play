using System.Collections.Generic;
using System.Linq;

public static class ESceneUtils
{
    public static EScene GetSceneByBuildIndex(int buildIndex)
    {
        List<EScene> matchingScenes = EnumUtils.GetValuesAsList<EScene>()
            .Where(sceneEnum => (int)sceneEnum == buildIndex)
            .ToList();
        if (matchingScenes.IsNullOrEmpty())
        {
            return EScene.OtherScene;
        }

        return matchingScenes.FirstOrDefault();
    }
}
