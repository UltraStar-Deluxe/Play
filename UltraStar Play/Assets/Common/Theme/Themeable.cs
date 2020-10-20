using System.Collections.Generic;
using UnityEngine;

abstract public class Themeable : MonoBehaviour
{
    abstract public void ReloadResources(Theme theme);

    private bool hasLoadedTheme;

    virtual protected void Start()
    {
        // In the normal game, the themes are loaded in during the LoadingScene,
        // such that they are always available later.
        if (!hasLoadedTheme
            && Application.isPlaying)
        {
            hasLoadedTheme = true;
            ReloadResources(ThemeManager.CurrentTheme);
        }
    }

#if UNITY_EDITOR
    abstract public List<UnityEngine.Object> GetAffectedObjects();
#endif
}
