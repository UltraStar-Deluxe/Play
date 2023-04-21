using System;
using UniRx;

public class ToggleControl
{
    private readonly Action onBecomeTrue;
    private readonly Action onBecomeFalse;
    
    public ReactiveProperty<bool> State { get; private set; }

    public ToggleControl(bool initialState, Action onBecomeTrue, Action onBecomeFalse)
    {
        this.onBecomeTrue = onBecomeTrue;
        this.onBecomeFalse = onBecomeFalse;
        State = new(initialState);
        State.Subscribe(_ => InvokeState());
    }

    public void ToggleState()
    {
        State.Value = !State.Value;
    }

    private void InvokeState()
    {
        if (State.Value)
        {
            onBecomeTrue?.Invoke();
        }
        else
        {
            onBecomeFalse?.Invoke();
        }
    }
}
