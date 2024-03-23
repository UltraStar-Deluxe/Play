using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public static class ObservableUtils
{
    public static IObservable<T> LogExceptionThenThrow<T>(Exception exception)
    {
        Debug.LogException(exception);
        return Observable.Throw<T>(exception);
    }

    public static IObservable<T> RunOnNewTaskAsObservable<T>(Func<T> function, IDisposable disposable = null)
    {
        disposable ??= Disposable.Empty;

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

    public static IObservable<T> RunOnNewTaskAsObservableElements<T>(Func<List<T>> function, IDisposable disposable = null)
    {
        disposable ??= Disposable.Empty;

        return Observable.Create<T>(o =>
        {
            Task.Run(() =>
            {
                try
                {
                    List<T> resultList = function();
                    if (!resultList.IsNullOrEmpty())
                    {
                        resultList.ForEach(result => o.OnNext(result));
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

    public static IObservable<T> RunOnNewTaskAsObservableElements<T>(Func<Task<List<T>>> function, IDisposable disposable = null)
    {
        disposable ??= Disposable.Empty;

        return Observable.Create<T>(o =>
        {
            Task.Run(async () =>
            {
                try
                {
                    List<T> resultList = await function();
                    if (!resultList.IsNullOrEmpty())
                    {
                        resultList.ForEach(result => o.OnNext(result));
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

    public static IObservable<List<T>> AllAtOnceUntilErrorOrCompleted<T>(IObservable<T> observable, bool logError = true)
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
