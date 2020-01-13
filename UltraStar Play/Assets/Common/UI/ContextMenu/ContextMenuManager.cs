using UnityEngine;

// Singleton that holds a reference to the context menu prefab.
// Otherwise the prefab would need be set for every implementation of AbstractContextMenuHandler separately,
// which is tedious and error prone.
public class ContextMenuManager : MonoBehaviour
{
    private static ContextMenuManager instance;
    public static ContextMenuManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = GameObjectUtils.FindComponentWithTag<ContextMenuManager>("ContextMenuManager");
            }
            return instance;
        }
    }

    public ContextMenu contextMenuPrefab;

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
