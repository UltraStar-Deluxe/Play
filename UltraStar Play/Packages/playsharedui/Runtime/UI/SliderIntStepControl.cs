using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SliderIntStepControl
{
    private int lastFrameCount = Time.frameCount;
    
    public SliderIntStepControl(SliderInt sliderInt, int step)
    {
        if (step <= 1)
        {
            return;
        }
        
        sliderInt.RegisterValueChangedCallback(evt =>
        {
            if (lastFrameCount == Time.frameCount)
            {
                return;
            }
            lastFrameCount = Time.frameCount;
            
            int newValue = evt.newValue;
            int roundedValue = Mathf.RoundToInt((float)newValue / step) * step;
            if (roundedValue != newValue)
            {
                // Set a new value next frame.
                // Otherwise the rounded value will be overwritten again with the non-rounded value.
                MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1,
                    () => sliderInt.value = roundedValue));
            }
        });
    }
}
