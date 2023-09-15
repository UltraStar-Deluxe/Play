using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class ObservableUtils
{
    public static IObservable<T> LogErrorThenThrow<T>(Exception exception)
    {
        Debug.LogError(exception.Message);
        return Observable.Throw<T>(exception);
    }

    public static IObservable<List<T>> AllItemsUntilErrorOrCompleted<T>(IObservable<T> observable, bool logError = true)
    {
        return Observable.Create<List<T>>(o =>
        {
            List<T> result = new();

            observable
                .CatchIgnore((Exception ex) =>
                {
                    if (logError)
                    {
                        Debug.LogException(ex);
                    }
                    o.OnNext(result);
                    o.OnCompleted();
                })
                .DoOnCompleted(() =>
                {
                    o.OnNext(result);
                    o.OnCompleted();
                })
                .Subscribe(item => result.Add(item));

            return Disposable.Empty;
        });
    }
}
