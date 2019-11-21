using UnityEngine;

public static class MoveAnchorsToCornersExtensions
{
    public static void MoveAnchorsToCorners_Width(this RectTransform rectTransform)
    {
        Vector2 anchorMinOld = rectTransform.anchorMin;
        Vector2 anchorMaxOld = rectTransform.anchorMax;
        Vector2 anchoredPositionOld = rectTransform.anchoredPosition;
        Vector2 sizeDeltaOld = rectTransform.sizeDelta;
        MoveAnchorsToCorners(rectTransform);
        rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, anchorMinOld.y);
        rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, anchorMaxOld.y);
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, anchoredPositionOld.y);
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, sizeDeltaOld.y);
    }

    public static void MoveAnchorsToCorners_Height(this RectTransform rectTransform)
    {
        Vector2 anchorMinOld = rectTransform.anchorMin;
        Vector2 anchorMaxOld = rectTransform.anchorMax;
        Vector2 anchoredPositionOld = rectTransform.anchoredPosition;
        Vector2 sizeDeltaOld = rectTransform.sizeDelta;
        MoveAnchorsToCorners(rectTransform);
        rectTransform.anchorMin = new Vector2(anchorMinOld.x, rectTransform.anchorMin.y);
        rectTransform.anchorMax = new Vector2(anchorMaxOld.x, rectTransform.anchorMax.y);
        rectTransform.anchoredPosition = new Vector2(anchoredPositionOld.x, rectTransform.anchoredPosition.y);
        rectTransform.sizeDelta = new Vector2(sizeDeltaOld.x, rectTransform.sizeDelta.y);
    }

    public static void MoveAnchorsToCorners(this RectTransform rectTransform)
    {
        RectTransform parentRectTransform = rectTransform.transform.parent.GetComponent<RectTransform>();
        Vector2 size = CalculateAnchorRectWidthAndHeight(rectTransform);
        Vector2 position = CalculateAnchorRectPosition(rectTransform, parentRectTransform, size.x, size.y);
        Rect anchorRect = new Rect(position.x, position.y, size.x, size.y);
        MoveAnchorsToCorners(rectTransform, parentRectTransform, anchorRect);
    }

    private static Vector2 CalculateAnchorRectWidthAndHeight(RectTransform ownRectTransform)
    {
        Vector2 size = new Vector2(ownRectTransform.rect.width, ownRectTransform.rect.height);
        return size;
    }

    private static Vector2 CalculateAnchorRectPosition(RectTransform ownRectTransform, RectTransform parentRectTransform, float width, float height)
    {
        Vector2 anchorVector = Vector2.zero;

        float pivotX = width * ownRectTransform.pivot.x;
        float pivotY = height * (1 - ownRectTransform.pivot.y);
        float newX = ownRectTransform.anchorMin.x * parentRectTransform.rect.width + ownRectTransform.offsetMin.x + pivotX - parentRectTransform.rect.width * anchorVector.x;
        float newY = -(1 - ownRectTransform.anchorMax.y) * parentRectTransform.rect.height + ownRectTransform.offsetMax.y - pivotY + parentRectTransform.rect.height * (1 - anchorVector.y);
        Vector2 position = new Vector2(newX, newY);
        return position;
    }

    private static void MoveAnchorsToCorners(RectTransform ownRectTransform, RectTransform parentRectTransform, Rect anchorRect)
    {
        Vector2 anchorVector = Vector2.zero;

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