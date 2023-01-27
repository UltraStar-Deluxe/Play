using UnityEngine;

public class DefaultSongSelectSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    public SceneData GetDefaultSceneData()
    {
        return new SongSelectSceneData();
    }
}
