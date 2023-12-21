using System;
using UniRx;

public class SongMetaChangeEventStream : IObservable<SongMetaChangeEvent>
{
    private readonly Subject<SongMetaChangeEvent> subject = new();

    public void OnNext(SongMetaChangeEvent value)
    {
        subject.OnNext(value);
    }

    public IDisposable Subscribe(IObserver<SongMetaChangeEvent> observer)
    {
        return subject.Subscribe(observer);
    }
}
