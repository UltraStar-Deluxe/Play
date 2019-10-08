using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

abstract public class Themeable : MonoBehaviour
{
    abstract public void ReloadResources();

    /// Looks for the color with the given name in the current theme and all parent themes.
    /// Returns true iff the color was found.
    protected bool TryLoadColorFromTheme(string colorName, out Color resultColor)
    {
        TextAsset textAsset = LoadAssetFromTheme<TextAsset>("colors");
        if (textAsset != null)
        {
            XElement xcolors = XElement.Parse(textAsset.text);
            foreach (XElement xcolor in xcolors.Elements("color"))
            {
                string loadedColorName = xcolor.Attribute("name").String();
                if (loadedColorName == colorName)
                {
                    string hexColorValue = xcolor.Value;
                    return ColorUtility.TryParseHtmlString("#" + hexColorValue, out resultColor);
                }
            }
        }
        resultColor = Color.black;
        return false;
    }

    /// Looks for the resource with the given name in the current theme and all parent themes.
    /// Returns null if the resource was not found. Otherwise the loaded resource is returned.
    protected T LoadAssetFromTheme<T>(string resourceName) where T : UnityEngine.Object
    {
        Theme currentTheme = ThemeManger.Instance.GetCurrentTheme();
        return LoadAssetFromTheme<T>(currentTheme, resourceName);
    }

    /// Looks for the resource with the given name in the specified theme.
    /// Returns null if the resource was not found. Otherwise the loaded resource is returned.
    private T LoadAssetFromTheme<T>(Theme theme, string resourceName) where T : UnityEngine.Object
    {
        if (theme == null)
        {
            return null;
        }

        string path = theme.Name + "/" + resourceName;
        T asset = Resources.Load<T>(path);
        if (asset != null)
        {
            return asset;
        }
        return LoadAssetFromTheme<T>(theme.ParentTheme, resourceName);
    }
}
