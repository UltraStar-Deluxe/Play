using System;
using UniRx;

public class AchievementEventStream : IDisposable, IObservable<AchievementId>
{
    public static AchievementEventStream Instance { get; private set; } = new();
    
    private readonly Subject<AchievementId> subject = new();

    private AchievementEventStream()
    {
        // private constructor for singleton.
    }
    
    public void Dispose()
    {
        subject.Dispose();
    }

    public void OnNext(AchievementId value)
    {
        subject.OnNext(value);
    }

    public IDisposable Subscribe(IObserver<AchievementId> observer)
    {
        return subject.Subscribe(observer);
    }
}
