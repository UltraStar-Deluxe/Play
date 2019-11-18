using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// Helper Component to move the anchors of a RectTransform to its corners.
/// This is triggered in a custom inspector on mouse-up event,
/// such that the anchors follow the corners after editing.
[ExecuteInEditMode]
[RequireComponent(typeof(RectTransform))]
public class AnchorsFollowCorners : MonoBehaviour
{
    public bool manualRefresh = true;
    private Rect anchorRect = new Rect(0, 0, 1, 1);
    private Vector2 anchorVector = Vector2.zero;
    private RectTransform ownRectTransform;
    private RectTransform parentRectTransform;

#if UNITY_EDITOR
    void Update()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (manualRefresh)
        {
            manualRefresh = false;
            MoveAnchorsToCorners();
        }
    }
#endif

    public void MoveAnchorsToCorners()
    {
        ownRectTransform = GetComponent<RectTransform>();
        parentRectTransform = transform.parent.GetComponent<RectTransform>();
        CalculateCurrentWH();
        CalculateCurrentXY();
        AnchorsToCorners();
    }

    private void CalculateCurrentXY()
    {
        float pivotX = anchorRect.width * ownRectTransform.pivot.x;
        float pivotY = anchorRect.height * (1 - ownRectTransform.pivot.y);
        float newX = ownRectTransform.anchorMin.x * parentRectTransform.rect.width + ownRectTransform.offsetMin.x + pivotX - parentRectTransform.rect.width * anchorVector.x;
        float newY = -(1 - ownRectTransform.anchorMax.y) * parentRectTransform.rect.height + ownRectTransform.offsetMax.y - pivotY + parentRectTransform.rect.height * (1 - anchorVector.y);
        Vector2 newXY = new Vector2(newX, newY);
        anchorRect.x = newXY.x;
        anchorRect.y = newXY.y;
    }

    private void CalculateCurrentWH()
    {
        anchorRect.width = ownRectTransform.rect.width;
        anchorRect.height = ownRectTransform.rect.height;
    }

    private void AnchorsToCorners()
    {
        float pivotX = anchorRect.width * ownRectTransform.pivot.x;
        float pivotY = anchorRect.height * (1 - ownRectTransform.pivot.y);
        ownRectTransform.anchorMin = new Vector2(0f, 1f);
        ownRectTransform.anchorMax = new Vector2(0f, 1f);

        float offsetMinX = anchorRect.x / ownRectTransform.localScale.x;
        float offsetMinY = anchorRect.y / ownRectTransform.localScale.y - anchorRect.height;
        ownRectTransform.offsetMin = new Vector2(offsetMinX, offsetMinY);
        float offsetMaxX = anchorRect.x / ownRectTransform.localScale.x + anchorRect.width;
        float offsetMaxY = anchorRect.y / ownRectTransform.localScale.y;
        ownRectTransform.offsetMax = new Vector2(offsetMaxX, offsetMaxY);

        float anchorMinX = ownRectTransform.anchorMin.x + anchorVector.x + (ownRectTransform.offsetMin.x - pivotX) / parentRectTransform.rect.width * ownRectTransform.localScale.x;
        float anchorMinY = ownRectTransform.anchorMin.y - (1 - anchorVector.y) + (ownRectTransform.offsetMin.y + pivotY) / parentRectTransform.rect.height * ownRectTransform.localScale.y;
        ownRectTransform.anchorMin = new Vector2(anchorMinX, anchorMinY);
        float anchorMaxX = ownRectTransform.anchorMax.x + anchorVector.x + (ownRectTransform.offsetMax.x - pivotX) / parentRectTransform.rect.width * ownRectTransform.localScale.x;
        float anchorMaxY = ownRectTransform.anchorMax.y - (1 - anchorVector.y) + (ownRectTransform.offsetMax.y + pivotY) / parentRectTransform.rect.height * ownRectTransform.localScale.y;
        ownRectTransform.anchorMax = new Vector2(anchorMaxX, anchorMaxY);

        offsetMinX = (0 - ownRectTransform.pivot.x) * anchorRect.width * (1 - ownRectTransform.localScale.x);
        offsetMinY = (0 - ownRectTransform.pivot.y) * anchorRect.height * (1 - ownRectTransform.localScale.y);
        ownRectTransform.offsetMin = new Vector2(offsetMinX, offsetMinY);
        offsetMaxX = (1 - ownRectTransform.pivot.x) * anchorRect.width * (1 - ownRectTransform.localScale.x);
        offsetMaxY = (1 - ownRectTransform.pivot.y) * anchorRect.height * (1 - ownRectTransform.localScale.y);
        ownRectTransform.offsetMax = new Vector2(offsetMaxX, offsetMaxY);
    }
}