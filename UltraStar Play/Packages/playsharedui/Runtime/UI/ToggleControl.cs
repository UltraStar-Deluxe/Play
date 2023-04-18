using System;

public class ToggleControl
{
    private readonly Action onBecomeTrue;
    private readonly Action onBecomeFalse;
    
    private bool state;
    public bool State
    {
        get => state;
        set
        {
            if (state == value)
            {
                return;
            }

            state = value;
            InvokeState();
        }
    }

    public ToggleControl(bool initialState, Action onBecomeTrue, Action onBecomeFalse, bool invokeInitialState = true)
    {
        this.onBecomeTrue = onBecomeTrue;
        this.onBecomeFalse = onBecomeFalse;
        state = initialState;

        if (invokeInitialState)
        {
            InvokeState();
        }
    }

    public void ToggleState()
    {
        State = !State;
    }

    private void InvokeState()
    {
        if (state)
        {
            onBecomeTrue?.Invoke();
        }
        else
        {
            onBecomeFalse?.Invoke();
        }
    }
}
