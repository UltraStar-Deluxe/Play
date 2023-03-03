using System.Linq;
using UnityEngine.SceneManagement;

public static class ESceneUtils
{
    public static EScene GetSceneByBuildIndex(int buildIndex)
    {
        return EnumUtils.GetValuesAsList<EScene>()
            .FirstOrDefault(sceneEnum => (int)sceneEnum == buildIndex);
    }
}
