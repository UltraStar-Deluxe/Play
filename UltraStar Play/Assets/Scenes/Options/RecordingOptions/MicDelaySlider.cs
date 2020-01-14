using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

public class MicDelaySlider : TextItemSlider<int>
{
    private IDisposable disposable;

    protected override void Awake()
    {
        base.Awake();

        List<int> values = new List<int>();
        for (int value = 0; value <= 500; value += 10)
        {
            values.Add(value);
        }

        Items = values;
    }

    public void SetMicProfile(MicProfile micProfile)
    {
        if (disposable != null)
        {
            disposable.Dispose();
        }

        Selection.Value = Items.Where(it => it == micProfile.DelayInMillis).FirstOrDefault().OrIfNull(140);
        disposable = Selection.Subscribe(newValue => micProfile.DelayInMillis = newValue);
    }

    protected override string GetDisplayString(int value)
    {
        return $"{value} ms";
    }
}