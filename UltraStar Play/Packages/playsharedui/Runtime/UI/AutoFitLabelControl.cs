using UnityEngine;
using UnityEngine.UIElements;

public class AutoFitLabelControl
{
    private const float AccuracyInPx = 2f;

    public float MinFontSizeInPx { get; set; }
    public float MaxFontSizeInPx { get; set; }
    public int MaxFontSizeIterations { get; set; } = 20;

    private readonly Label labelElement;

    public AutoFitLabelControl(Label labelElement, float minFontSizeInPx = 10, float maxFontSizeInPx = 50)
    {
        this.labelElement = labelElement;
        this.MinFontSizeInPx = minFontSizeInPx;
        this.MaxFontSizeInPx = maxFontSizeInPx;
        this.labelElement.RegisterCallback<GeometryChangedEvent>(evt => UpdateFontSize());
        this.labelElement.RegisterValueChangedCallback(evt => UpdateFontSize());
    }

    public void UpdateFontSize()
    {
        SetBestFitFontSize(labelElement, MinFontSizeInPx, MaxFontSizeInPx, MaxFontSizeIterations);
    }
    
    public static void SetBestFitFontSize(Label labelElement, float minFontSizeInPx, float maxFontSizeInPx, int maxFontSizeIterations)
    {
        Rect labelElementContentRect = labelElement.contentRect;
        if (float.IsNaN(labelElementContentRect.width)
            || float.IsNaN(labelElementContentRect.height)
            || labelElementContentRect.width <= 0
            || labelElementContentRect.height <= 0)
        {
            // Cannot calculate font size yet.
            return;
        }

        // Binary search on font size
        float lastFontSizeInPx = -1;
        float fromFontSizeInPx = minFontSizeInPx;
        float untilFontSizeInPx = maxFontSizeInPx;
        float nextFontSizeInPx = labelElement.resolvedStyle.fontSize;
        
        for (int i = 0; i < maxFontSizeIterations; i++)
        {
            Vector2 preferredSize = labelElement.MeasureTextSize(labelElement.text,
                0, VisualElement.MeasureMode.Undefined,
                0, VisualElement.MeasureMode.Undefined);

            if (Mathf.Abs(preferredSize.x - labelElementContentRect.width) < AccuracyInPx
                && Mathf.Abs(preferredSize.y - labelElementContentRect.height) < AccuracyInPx)
            {
                // Font size is already good enough.
                return;
            }
            
            if (preferredSize.x > labelElementContentRect.width
                || preferredSize.y > labelElementContentRect.height)
            {
                // Text is too big, reduce font size
                untilFontSizeInPx = nextFontSizeInPx;
                nextFontSizeInPx = fromFontSizeInPx + (untilFontSizeInPx - fromFontSizeInPx) / 2;
            }
            else
            {
                // Text is too small, increase font size
                fromFontSizeInPx = nextFontSizeInPx;
                nextFontSizeInPx = fromFontSizeInPx + (untilFontSizeInPx - fromFontSizeInPx) / 2;
            }

            nextFontSizeInPx = NumberUtils.Limit(nextFontSizeInPx, minFontSizeInPx, maxFontSizeInPx);
            
            // Use a whole number for font size, otherwise Unity may be struggling to layout the text.
            nextFontSizeInPx = (int)nextFontSizeInPx;
            
            if (lastFontSizeInPx >= 0 && Mathf.Abs(lastFontSizeInPx - nextFontSizeInPx) < 0.5f)
            {
                // Font size is already good enough.
                return;
            }

            labelElement.style.fontSize = nextFontSizeInPx;
            
            lastFontSizeInPx = nextFontSizeInPx;
        }
    }
}
