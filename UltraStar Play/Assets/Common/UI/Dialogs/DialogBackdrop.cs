using System;
using UnityEngine;
using UnityEngine.UI;

public class DialogBackdrop : MonoBehaviour
{
    public Image background;
    public Color targetColor = new Color32(0, 0, 0, 90);
    public float colorTransitionTime = 1f;

    private float startTime;
    private Color startColor;
    private Vector2Int lastScreenSize;
    
    public void Start()
    {
        startTime = Time.time;
        startColor = background.color;
        FitToScreenSize();
    }

    private void Update()
    {
        float progress = (Time.time - startTime) / colorTransitionTime;
        background.color = Color.Lerp(startColor, targetColor, progress);

        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            FitToScreenSize();
        }
    }

    private void FitToScreenSize()
    {
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        // Make the background match the screen size, regardless of its parent.
        RectTransform backgroundTransform = background.GetComponent<RectTransform>();
        backgroundTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width);
        backgroundTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.height);
    }
}
