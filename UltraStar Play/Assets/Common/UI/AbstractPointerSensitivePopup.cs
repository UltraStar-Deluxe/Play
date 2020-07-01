using UnityEngine;
using UnityEngine.UI;

public class AbstractPointerSensitivePopup : MonoBehaviour
{
    public RectTransform RectTransform { get; private set; }

    private float lastWidth;
    private float lastHeight;

    protected virtual void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    protected virtual void Update()
    {
        MoveInsideScreenBorders();
    }

    protected void MoveInsideScreenBorders()
    {
        // Move up and left if out of screen
        if (lastWidth != RectTransform.rect.width)
        {
            lastWidth = RectTransform.rect.width;

            float x = RectTransform.position.x;
            float xOvershoot = (x + RectTransform.rect.width) - Screen.width;
            if (xOvershoot > 0)
            {
                RectTransform.position = new Vector2(x - xOvershoot, RectTransform.position.y);
            }
        }
        if (lastHeight != RectTransform.rect.height)
        {
            lastHeight = RectTransform.rect.height;

            float y = RectTransform.position.y;
            float yOvershoot = (RectTransform.rect.height - y);
            if (yOvershoot > 0)
            {
                RectTransform.position = new Vector2(RectTransform.position.x, y + yOvershoot);
            }
        }
    }
}
