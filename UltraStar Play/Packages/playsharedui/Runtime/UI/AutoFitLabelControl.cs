using UnityEngine;
using UnityEngine.UIElements;

public class AutoFitLabelControl
{
    public float MinFontSizeInPx { get; set; }
    public float MaxFontSizeInPx { get; set; }
    public int MaxFontSizeIterations { get; set; } = 20;

    private readonly Label labelElement;

    private float startFontSizeInPx = -1;

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
        if (float.IsNaN(labelElement.contentRect.width)
            || float.IsNaN(labelElement.contentRect.height))
        {
            // Cannot calculate font size yet.
            return;
        }
        
        if (startFontSizeInPx < 0)
        {
            startFontSizeInPx = labelElement.resolvedStyle.fontSize;
        }

        // Binary search on font size
        float lastFontSizeInPx = -1;
        float fromFontSizeInPx = MinFontSizeInPx;
        float untilFontSizeInPx = MaxFontSizeInPx;
        float nextFontSizeInPx = labelElement.resolvedStyle.fontSize;
        
        for (int i = 0; i < MaxFontSizeIterations; i++)
        {
            Vector2 preferredSize = labelElement.MeasureTextSize(labelElement.text,
                0, VisualElement.MeasureMode.Undefined,
                0, VisualElement.MeasureMode.Undefined);

            if (Mathf.Abs(preferredSize.x - labelElement.contentRect.width) < 1f
                && Mathf.Abs(preferredSize.y - labelElement.contentRect.height) < 1f)
            {
                // Font size is already good enough.
                return;
            }
            
            if (preferredSize.x > labelElement.contentRect.width
                || preferredSize.y > labelElement.contentRect.height)
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

            nextFontSizeInPx = NumberUtils.Limit(nextFontSizeInPx, MinFontSizeInPx, MaxFontSizeInPx);
            labelElement.style.fontSize = nextFontSizeInPx;
            
            if (lastFontSizeInPx >= 0
                && Mathf.Abs(lastFontSizeInPx - nextFontSizeInPx) < 0.5f)
            {
                // Font size is already good enough.
                return;
            }

            lastFontSizeInPx = nextFontSizeInPx;
        }
    }
}
