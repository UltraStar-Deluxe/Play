 using UnityEngine;
 using UnityEngine.UIElements;

 public class AutoFitLabelControl
 {
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
         if (float.IsNaN(labelElement.contentRect.width)
             || float.IsNaN(labelElement.contentRect.height))
         {
             // Cannot calculate font size yet.
             return;
         }

         float nextFontSizeInPx;
         int direction;
         int lastDirection = 0;
         float step = 1;
         int loop = 0;

         while (loop < MaxFontSizeIterations)
         {
             Vector2 preferredSize = labelElement.MeasureTextSize(labelElement.text,
                 0, VisualElement.MeasureMode.Undefined,
                 0, VisualElement.MeasureMode.Undefined);

             if (preferredSize.x > labelElement.contentRect.width
                 || preferredSize.y > labelElement.contentRect.height)
             {
                 // Text is too big, reduce font size
                 direction = -1;
             }
             else
             {
                 // Text is too small, increase font size
                 direction = 1;
             }

             if (lastDirection != 0
                 && direction != lastDirection)
             {
                 // Found best match.
                 return;
             }
             lastDirection = direction;

             nextFontSizeInPx = labelElement.resolvedStyle.fontSize + (step * direction);
             nextFontSizeInPx = NumberUtils.Limit(nextFontSizeInPx, MinFontSizeInPx, MaxFontSizeInPx);
             labelElement.style.fontSize = nextFontSizeInPx;
             loop++;
         }
     }
 }
