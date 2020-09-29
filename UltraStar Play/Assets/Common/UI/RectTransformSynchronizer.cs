using UnityEngine;

[ExecuteInEditMode]
public class RectTransformSynchronizer : MonoBehaviour
{
    public RectTransform target;

    public bool synchX = true;
    public bool synchY = true;
    public bool synchWidth = true;
    public bool synchHeight = true;

    public bool onEnable = true;
    public bool onUpdate = true;

    private RectTransform rectTransform;

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        if (!onEnable || target == null)
        {
            return;
        }
        CopyTargetRectTransformValues();
    }

    private void Update()
    {
        if (!onUpdate || target == null)
        {
            return;
        }
        CopyTargetRectTransformValues();
    }

    private void CopyTargetRectTransformValues()
    {
        float targetX = synchX ? target.position.x : rectTransform.position.x;
        float targetY = synchY ? target.position.y : rectTransform.position.y;
        if ((targetX != rectTransform.position.x)
            || (targetY != rectTransform.position.y))
        {
            rectTransform.position = new Vector3(targetX, targetY, 0);
        }

        float targetWidth = synchWidth ? target.rect.width : rectTransform.rect.width;
        if (targetWidth != rectTransform.rect.width)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        }
        float targetHeight = synchHeight ? target.rect.height : rectTransform.rect.height;
        if (targetHeight != rectTransform.rect.height)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        }
    }
}
