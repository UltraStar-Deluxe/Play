using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnchorsFollowCorners))]
public class AnchorsFollowCornersInspector : EditorBase
{
    AnchorsFollowCorners myTarget;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        myTarget = target as AnchorsFollowCorners;
    }

    void OnSceneGUI()
    {
        if (Event.current == null || myTarget == null)
        {
            return;
        }

        if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
        {
            myTarget.MoveAnchorsToCorners();
        }
    }
}
