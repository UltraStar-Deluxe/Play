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

        sliderInt.RegisterValueChangedCallback(async evt =>
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
                // Set a new value next frame to avoid infinite callback loop.
                await Awaitable.NextFrameAsync();
                sliderInt.value = roundedValue;
            }
        });
    }
}
