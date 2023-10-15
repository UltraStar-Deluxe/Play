using System;
using UniRx;

public static class CustomObservableExtensions
{
    public static IDisposable SubscribeOneShot<T>(
        this IObservable<T> observable,
        Action<T> onNext)
    {
        IDisposable subscriptionDisposable = null;
        subscriptionDisposable = observable
            .Subscribe(evt =>
            {
                subscriptionDisposable?.Dispose();
                onNext(evt);
            });
        return subscriptionDisposable;
    }
}
