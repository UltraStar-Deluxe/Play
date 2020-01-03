using System;
using UniRx;

public class SongMetaChangeEventStream : IDisposable, IObservable<ISongMetaChangeEvent>
{
    private readonly Subject<ISongMetaChangeEvent> subject = new Subject<ISongMetaChangeEvent>();

    public void Dispose()
    {
        subject.Dispose();
    }

    public void OnNext(ISongMetaChangeEvent value)
    {
        subject.OnNext(value);
    }

    public IDisposable Subscribe(IObserver<ISongMetaChangeEvent> observer)
    {
        return subject.Subscribe(observer);
    }
}