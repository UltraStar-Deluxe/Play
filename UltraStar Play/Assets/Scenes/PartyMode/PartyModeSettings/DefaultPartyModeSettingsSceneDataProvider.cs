using System.Linq;
using UnityEngine;

public class DefaultPartyModeSettingsSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    public EPartyModeType mode;

    public SceneData GetDefaultSceneData()
    {
        PartyModeSettingsSceneData data = new();
        data.Mode = mode;
        return data;
    }
}
