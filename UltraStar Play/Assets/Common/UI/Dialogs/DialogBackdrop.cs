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
    
    public void Start()
    {
        startTime = Time.time;
        startColor = background.color;
        
        // Make the background match the screen size, regardless of its parent.
        RectTransform backgroundTransform = background.GetComponent<RectTransform>();
        backgroundTransform.position = new Vector2(0, 0);
        backgroundTransform.anchorMax = new Vector2(Screen.width, Screen.height);
    }

    private void Update()
    {
        float progress = (Time.time - startTime) / colorTransitionTime;
        background.color = Color.Lerp(startColor, targetColor, progress);
    }
}
