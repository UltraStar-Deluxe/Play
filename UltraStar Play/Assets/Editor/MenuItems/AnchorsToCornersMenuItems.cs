using UnityEditor;
using UnityEngine;

public static class AnchorsToCornersMenuItems
{
    // Hotkey: Alt+A
    [MenuItem("Tools/Anchors to Corners (RectTransform)/Width and Height &a")]
    public static void MoveAnchorsToCorners()
    {
        EditorUtils.GetSelectedComponents<RectTransform>().ForEach(it =>
        {
            Undo.RecordObject(it, "MoveAnchorsToCorners");
            MoveAnchorsToCornersExtensions.MoveAnchorsToCorners(it);
        });
    }

    [MenuItem("Tools/Anchors to Corners (RectTransform)/Width")]
    public static void MoveAnchorsToCorners_Width()
    {
        EditorUtils.GetSelectedComponents<RectTransform>().ForEach(it =>
        {
            Undo.RecordObject(it, "MoveAnchorsToCorners_Width");
            MoveAnchorsToCornersExtensions.MoveAnchorsToCorners_Width(it);
        });
    }

    [MenuItem("Tools/Anchors to Corners (RectTransform)/Height")]
    public static void MoveAnchorsToCorners_Height()
    {
        EditorUtils.GetSelectedComponents<RectTransform>().ForEach(it =>
        {
            Undo.RecordObject(it, "MoveAnchorsToCorners_Height");
            MoveAnchorsToCornersExtensions.MoveAnchorsToCorners_Height(it);
        });
    }
}