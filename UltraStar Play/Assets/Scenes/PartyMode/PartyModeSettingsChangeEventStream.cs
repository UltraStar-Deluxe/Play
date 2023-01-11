using System;
using UniRx;

public class PartyModeSettingsChangeEventStream : IDisposable, IObservable<PartyModeSettingsChangeEvent>
{
    private readonly Subject<PartyModeSettingsChangeEvent> subject = new();

    public void Dispose()
    {
        subject.Dispose();
    }

    public void OnNext(PartyModeSettingsChangeEvent value)
    {
        subject.OnNext(value);
    }

    public IDisposable Subscribe(IObserver<PartyModeSettingsChangeEvent> observer)
    {
        return subject.Subscribe(observer);
    }
}
