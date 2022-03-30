using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

public class DetectBpmControl
{
    private int clickCount;
    private float startTime;
    private float lastClickTime;

    public DetectBpmControl(Button button, Label label)
    {
        button.RegisterCallbackButtonTriggered(() =>
        {
            // Automatically reset the counter after a very long delay
            if (Time.time - lastClickTime > 2)
            {
                ResetCount();
            }

            if (clickCount == 0)
            {
                startTime = Time.time;
                label.text = "First clicks";
            }
            else
            {
                float durationInSeconds = Time.time - startTime;
                if (durationInSeconds > 1)
                {
                    float bpm = 60 * clickCount / durationInSeconds;
                    label.text = $"{bpm.ToString("0.00", CultureInfo.InvariantCulture)} BPM";
                }
            }

            // Incrementing the clicks must be done after calculating the duration
            // (on first click, there is no duration yet)
            clickCount++;

            lastClickTime = Time.time;
        });
    }

    private void ResetCount()
    {
        clickCount = 0;
    }
}
