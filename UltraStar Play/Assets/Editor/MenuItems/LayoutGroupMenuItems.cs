using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using System;

public class LayoutGroupMenuItems : MonoBehaviour
{
    [MenuItem("Tools/Layout Groups/Add LayoutElement")]
    public static void AddLayoutElement()
    {
        GetSelectedGameObjectsWithoutComponent<LayoutElement>().ForEach(gameObject =>
        {
            LayoutElement layoutElement = Undo.AddComponent<LayoutElement>(gameObject);
            layoutElement.minWidth = 0;
            layoutElement.minHeight = 0;

            layoutElement.preferredWidth = 0;
            layoutElement.preferredHeight = 0;

            layoutElement.flexibleWidth = 1;
            layoutElement.flexibleHeight = 1;

            layoutElement.layoutPriority = 1;
        });
    }

    [MenuItem("Tools/Layout Groups/Add VerticalLayoutGroup")]
    public static void AddVerticalLayoutGroup()
    {
        GetSelectedGameObjectsWithoutComponent<VerticalLayoutGroup>().ForEach(gameObject =>
        {
            VerticalLayoutGroup layoutGroup = Undo.AddComponent<VerticalLayoutGroup>(gameObject);
            ConfigureHorizontalOrVerticalLayoutGroup(layoutGroup);
        });
    }

    [MenuItem("Tools/Layout Groups/Add HorizontalLayoutGroup")]
    public static void AddHorizontalLayoutGroup()
    {
        GetSelectedGameObjectsWithoutComponent<HorizontalLayoutGroup>().ForEach(gameObject =>
        {
            HorizontalLayoutGroup layoutGroup = Undo.AddComponent<HorizontalLayoutGroup>(gameObject);
            ConfigureHorizontalOrVerticalLayoutGroup(layoutGroup);
        });
    }

    [MenuItem("Tools/Layout Groups/Add GridLayoutGroup")]
    public static void AddGridLayoutGroup()
    {
        GetSelectedGameObjectsWithoutComponent<GridLayoutGroup>().ForEach(gameObject =>
        {
            Undo.AddComponent<GridLayoutGroup>(gameObject);
            GridLayoutGroupCellSizer layoutGroupCellSizer = Undo.AddComponent<GridLayoutGroupCellSizer>(gameObject);
            layoutGroupCellSizer.columns = 3;
            layoutGroupCellSizer.rows = 3;
        });
    }

    private static void ConfigureHorizontalOrVerticalLayoutGroup(HorizontalOrVerticalLayoutGroup layoutGroup)
    {
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;

        layoutGroup.childScaleWidth = false;
        layoutGroup.childScaleHeight = false;

        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = true;
    }

    private static IEnumerable<GameObject> GetSelectedGameObjectsWithoutComponent<T>()
    {
        return Selection.gameObjects.Where(gameObject => gameObject.GetComponent<T>() == null);
    }
}
