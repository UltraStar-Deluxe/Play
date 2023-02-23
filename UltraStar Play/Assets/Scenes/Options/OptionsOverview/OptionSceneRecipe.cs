using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class OptionSceneRecipe
{
    public EScene scene;
    public VisualTreeAsset visualTreeAsset;
    public List<GameObject> sceneGameObjects;

    public override string ToString()
    {
        return StringUtils.ToTitleCase(scene.ToString());
    }
}
