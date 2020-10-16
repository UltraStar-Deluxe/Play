using UnityEngine;

abstract public class Themeable : MonoBehaviour
{
    abstract public void ReloadResources(Theme theme);

#if UNITY_EDITOR
    private bool hasLoadedTheme;
    virtual protected void Update()
    {
        if (!hasLoadedTheme
            && ThemeManager.HasFinishedLoadingThemes)
        {
            hasLoadedTheme = true;
            ReloadResources(ThemeManager.CurrentTheme);
        }
    }
#else
    // In the normal game, the themes are loaded in during the LoadingScene,
    // such that they are always available later.
    protected void Start()
    {
        ReloadResources(ThemeManager.CurrentTheme);
    }
#endif
}
