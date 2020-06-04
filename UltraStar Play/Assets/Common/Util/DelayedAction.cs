using System;

// Utility class that performs an action after some time has passed.
public class DelayedAction
{
    private float currentTime;

    public void CountTimeAndRunDelayed(float delay, Action action)
    {
        CountTimeAndRunDelayed(1, delay, action);
    }

    public void CountTimeAndRunDelayed(float deltaTime, float delay, Action action)
    {
        currentTime += deltaTime;
        if (currentTime > delay)
        {
            action();
        }
    }
}
