using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public class ObservableUtils
{
    public static IObservable<T> LogErrorThenThrow<T>(Exception exception)
    {
        Debug.LogError(exception.Message);
        return Observable.Throw<T>(exception);
    }

    public static IObservable<T> RunOnNewTaskAsObservable<T>(Func<T> function, IDisposable disposable)
    {
        return Observable.Create<T>(o =>
        {
            Task.Run(() =>
            {
                try
                {
                    T result = function();
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

    public static IObservable<T> RunOnNewTaskAsObservable<T>(Func<Task<T>> function, IDisposable disposable)
    {
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
