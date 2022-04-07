using System;
using UniRx;
using UnityEngine;

public static class UniRxExtensions
{

    public static IObservable<T> WhereNotNull<T>(this IObservable<T> source)
    {
        return source.Where(x => x != null);
    }

    public static IDisposable SubscribeAndAddToGameObject<T>(this IObservable<T> source, Action<T> onNext, GameObject gameObject)
    {
        return source.Subscribe(onNext).AddTo(gameObject);
    }
}
