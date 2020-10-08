using System;
using UniRx;

public class SongMetaChangeEventStream : IDisposable, IObservable<SongMetaChangeEvent>
{
    private readonly Subject<SongMetaChangeEvent> subject = new Subject<SongMetaChangeEvent>();

    public void Dispose()
    {
        subject.Dispose();
    }

    public void OnNext(SongMetaChangeEvent value)
    {
        subject.OnNext(value);
    }

    public IDisposable Subscribe(IObserver<SongMetaChangeEvent> observer)
    {
        return subject.Subscribe(observer);
    }
}