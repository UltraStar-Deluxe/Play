using System;
using UniRx;

public class AchievementEventStream : IDisposable, IObservable<AchievementEvent>
{
    public static AchievementEventStream Instance { get; private set; } = new();

    private readonly Subject<AchievementEvent> subject = new();

    private AchievementEventStream()
    {
        // private constructor for singleton.
    }

    public void Dispose()
    {
        subject.Dispose();
    }

    public void OnNext(AchievementEvent value)
    {
        subject.OnNext(value);
    }

    public IDisposable Subscribe(IObserver<AchievementEvent> observer)
    {
        return subject.Subscribe(observer);
    }
}
