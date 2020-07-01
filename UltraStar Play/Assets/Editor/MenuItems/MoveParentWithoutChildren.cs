using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[InitializeOnLoad]
public static class MoveParentWithoutAffectingChildren
{
    private static readonly KeyCode[] keyCodes = new KeyCode[] { KeyCode.F4, KeyCode.RightControl };
    private static readonly Dictionary<GameObject, PositionMemento> childToPositionMemento = new Dictionary<GameObject, PositionMemento>();

    private static bool isKeyDown;

    static MoveParentWithoutAffectingChildren()
    {
        SceneView.duringSceneGui += CustomOnSceneGuiCallback;
    }

    private static void CustomOnSceneGuiCallback(SceneView sceneView)
    {
        Event e = Event.current;
        if (e != null && keyCodes.Contains(e.keyCode))
        {
            if (e.type == EventType.KeyDown && !isKeyDown)
            {
                isKeyDown = true;
                OnStartMoveOfParentWithoutAffectingChildren();
            }
            else if (e.type == EventType.KeyUp)
            {
                isKeyDown = false;
                OnEndMoveOfParentWithoutAffectingChildren();
            }
        }
    }

    private static void OnStartMoveOfParentWithoutAffectingChildren()
    {
        // Store the current position and size of direct children in selected GameObjects
        childToPositionMemento.Clear();
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        foreach (GameObject parentGameObject in Selection.gameObjects)
        {
            Undo.RegisterFullObjectHierarchyUndo(parentGameObject, "OnStartMoveOfParentWithoutAffectingChildren");
            foreach (Transform childTransform in parentGameObject.transform)
            {
                childToPositionMemento[childTransform.gameObject] = new PositionMemento(childTransform);
            }
        }
    }

    private static void OnEndMoveOfParentWithoutAffectingChildren()
    {
        // Restore the position and size
        foreach (GameObject childGameObject in childToPositionMemento.Keys)
        {
            if (childToPositionMemento.TryGetValue(childGameObject, out PositionMemento positionMemento))
            {
                positionMemento.Restore(childGameObject.transform);
            }
        }
    }

    private class PositionMemento
    {
        private readonly Vector3 position;
        private readonly float width;
        private readonly float height;

        public PositionMemento(Transform transform)
        {
            position = transform.position;
            RectTransform rectTransform = transform.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                width = rectTransform.rect.width;
                height = rectTransform.rect.height;
            }
        }

        public void Restore(Transform transform)
        {
            transform.position = position;
            RectTransform rectTransform = transform.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
        }
    }
}
