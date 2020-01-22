using UnityEngine;

/// Mimics several fields of a RectTransform for easier editing in the Inspector.
/// Some of these fields are sometimes unavailable in the normal Inspector for a RectTransform.
/// Notably the absolute size of a RectTransform can be set much more easily with this helper.
[RequireComponent(typeof(RectTransform))]
[ExecuteInEditMode]
public class RectTransformHelper : MonoBehaviour
{
    // Fields of the RectTransform
    // that should be applied to the RectTransform when it is changed in this script.
    public Vector2 anchorMin;
    private Vector2 anchorMinOld;

    public Vector2 anchorMax;
    private Vector2 anchorMaxOld;

    public Vector2 pivot;
    private Vector2 pivotOld;

    public Vector3 localPosition;
    private Vector3 localPositionOld;

    public Vector2 anchoredPosition;
    private Vector2 anchoredPositionOld;

    public Vector3 position;
    private Vector3 positionOld;

    public Vector2 sizeDelta;
    private Vector2 sizeDeltaOld;

    public Vector2 size;
    private Vector2 sizeOld;

    // The RectTransform to apply any changes to
    private RectTransform rectTransform;

    void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();

        // Initialize the current and old values to the values of the RectTransform
        anchorMin = rectTransform.anchorMin;
        anchorMinOld = anchorMin;

        anchorMax = rectTransform.anchorMax;
        anchorMaxOld = anchorMax;

        pivot = rectTransform.pivot;
        pivotOld = pivot;

        localPosition = rectTransform.localPosition;
        localPositionOld = localPosition;

        anchoredPosition = rectTransform.anchoredPosition;
        anchoredPositionOld = anchoredPosition;

        position = rectTransform.position;
        positionOld = position;

        sizeDelta = rectTransform.sizeDelta;
        sizeDeltaOld = sizeDelta;

        size = new Vector2(rectTransform.rect.width, rectTransform.rect.height);
        sizeOld = size;
    }

    void Update()
    {
        // Apply changes of this script to the RectTransform.
        // This must be done when (current-value != old-value) but (current-value == value-of-RectTransform).
        // Furthermore, apply changes of the RectTransform to this script.
        // This must be done when (current-value != value-of-RectTransform) but (current-value == old-value).
        UpdateAnchorMax();
        UpdateAnchorMin();
        UpdatePivot();
        UpdateAnchoredPosition();
        UpdateLocalPosition();
        UpdatePosition();
        UpdateSizeDelta();
        UpdateSize();
    }

    private void UpdateSize()
    {
        if (sizeOld != size)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }
        else if (rectTransform.rect.width != size.x || rectTransform.rect.height != size.y)
        {
            size = new Vector2(rectTransform.rect.width, rectTransform.rect.height);
        }
        sizeOld = size;
    }

    private void UpdateSizeDelta()
    {
        if (sizeDeltaOld != sizeDelta)
        {
            rectTransform.sizeDelta = sizeDelta;
        }
        else if (rectTransform.sizeDelta != sizeDelta)
        {
            sizeDelta = rectTransform.sizeDelta;
        }
        sizeDeltaOld = sizeDelta;
    }

    private void UpdatePosition()
    {
        if (positionOld != position)
        {
            rectTransform.position = position;
        }
        else if (rectTransform.position != position)
        {
            position = rectTransform.position;
        }
        positionOld = position;
    }

    private void UpdateLocalPosition()
    {
        if (localPositionOld != localPosition)
        {
            rectTransform.localPosition = localPosition;
        }
        else if (rectTransform.localPosition != localPosition)
        {
            localPosition = rectTransform.localPosition;
        }
        localPositionOld = localPosition;
    }

    private void UpdateAnchoredPosition()
    {
        if (anchoredPositionOld != anchoredPosition)
        {
            rectTransform.anchoredPosition = anchoredPosition;
        }
        else if (rectTransform.anchoredPosition != anchoredPosition)
        {
            anchoredPosition = rectTransform.anchoredPosition;
        }
        anchoredPositionOld = anchoredPosition;
    }

    private void UpdatePivot()
    {
        if (pivotOld != pivot)
        {
            rectTransform.pivot = pivot;
        }
        else if (rectTransform.pivot != pivot)
        {
            pivot = rectTransform.pivot;
        }
        pivotOld = pivot;
    }

    private void UpdateAnchorMin()
    {
        if (anchorMinOld != anchorMin)
        {
            rectTransform.anchorMin = anchorMin;
        }
        else if (rectTransform.anchorMin != anchorMin)
        {
            anchorMin = rectTransform.anchorMin;
        }
        anchorMinOld = anchorMin;
    }

    private void UpdateAnchorMax()
    {
        if (anchorMaxOld != anchorMax)
        {
            rectTransform.anchorMax = anchorMax;
        }
        else if (rectTransform.anchorMax != anchorMax)
        {
            anchorMax = rectTransform.anchorMax;
        }
        anchorMaxOld = anchorMax;
    }
}
