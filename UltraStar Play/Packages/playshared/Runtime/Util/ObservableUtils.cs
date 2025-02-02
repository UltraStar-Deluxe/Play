using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public static class ObservableUtils
{
    public static IObservable<T> RunOnNewTaskAsObservable<T>(Func<Task<T>> function, IDisposable disposable = null)
    {
        disposable = disposable ?? Disposable.Empty;

        return Observable.Create<T>(o =>
        {
            Task.Run(async () =>
            {
                try
                {
                    T result = await function();
                    if (result != null)
                    {
                        o.OnNext(result);
                    }
                    o.OnCompleted();
                }
                catch (Exception ex)
                {
                    o.OnError(ex);
                }

                return disposable;
            });

            return disposable;
        });
    }
}
