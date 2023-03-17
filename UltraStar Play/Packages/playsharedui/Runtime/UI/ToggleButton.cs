using System;
using UniRx;
using UnityEngine.UIElements;

public class ToggleButton : Button
{
    public new class UxmlFactory : UxmlFactory<ToggleButton, UxmlTraits> { }
    public new class UxmlTraits : Button.UxmlTraits { }
    
    public bool IsActive => ClassListContains("active");

    // TODO: How to send custom events in UIToolkit? See https://forum.unity.com/threads/how-can-i-dispatch-custom-event-in-uitoolkit.1340549/#post-8464325
    private readonly Subject<bool> isActiveChangedEventStream = new();
    public IObservable<bool> IsActiveChangedEventStream => isActiveChangedEventStream;

    public ToggleButton()
    {
        AddToClassList("toggleButton");
    }

    public void ToggleActive()
    {
        SetActive(!IsActive);
    }
    
    public void SetActive(bool state)
    {
        if (state && !IsActive)
        {
            AddToClassList("active");
            isActiveChangedEventStream.OnNext(true);
        }
        else if(!state && IsActive)
        {
            RemoveFromClassList("active");
            isActiveChangedEventStream.OnNext(false);
        }
    }
}
