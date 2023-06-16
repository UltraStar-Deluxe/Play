using System;
using UniRx;
using UnityEngine;

public class ObservableUtils
{
    public static IObservable<T> LogErrorThenThrow<T>(Exception exception)
    {
        Debug.LogError(exception.Message);
        return Observable.Throw<T>(exception);
    }
}
