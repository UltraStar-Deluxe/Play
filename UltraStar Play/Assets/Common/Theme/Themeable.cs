using UnityEngine;

abstract public class Themeable : MonoBehaviour
{
    abstract public void ReloadResources(Theme theme);

    protected string GetStreamingAssetsUri(Theme theme, string resourceName)
    {
        string resourcePath = ThemeManager.themesFolderName + "/" + theme.Name + "/" + resourceName;
        return ApplicationUtils.GetStreamingAssetsUri(resourcePath);
    }
}
