using System;
using UniRx;

public class SongMetaChangedEventStream : IObservable<SongMetaChangedEvent>
{
    private readonly Subject<SongMetaChangedEvent> subject = new();

    public void OnNext(SongMetaChangedEvent value)
    {
        subject.OnNext(value);
    }

    public IDisposable Subscribe(IObserver<SongMetaChangedEvent> observer)
    {
        return subject.Subscribe(observer);
    }
}
