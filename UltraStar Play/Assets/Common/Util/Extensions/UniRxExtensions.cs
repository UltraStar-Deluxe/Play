using System;
using UniRx;

public static class UniRxExtensions
{

    public static IObservable<T> WhereNotNull<T>(this IObservable<T> source)
    {
        return source.Where(x => x != null);
    }

}