using System;
using UniRx;

public static class CommonEventStream
{
    private static readonly Subject<ICommonEvent> subject = new();

    public static void Publish(ICommonEvent value)
    {
        subject.OnNext(value);
    }

    public static IDisposable Subscribe<T>(IObserver<T> observer)
    {
        return subject
            .OfType<ICommonEvent, T>()
            .Subscribe(observer);
    }

    public static IDisposable Subscribe<T>(Action<T> onNext, Action<Exception> onError = null, Action onCompleted = null)
    {
        return Subscribe<T>(new ObserverFromActions<T>(onNext, onError, onCompleted));
    }

    private class ObserverFromActions<T> : IObserver<T>
    {
        private readonly Action<T> onNext;
        private readonly Action<Exception> onError;
        private readonly Action onCompleted;

        public ObserverFromActions(
            Action<T> onNext,
            Action<Exception> onError = null,
            Action onCompleted = null)
        {
            this.onNext = onNext;
            this.onError = onError;
            this.onCompleted = onCompleted;
        }

        public void OnCompleted()
        {
            onCompleted?.Invoke();
        }

        public void OnError(Exception error)
        {
            onError?.Invoke(error);
        }

        public void OnNext(T value)
        {
            onNext?.Invoke(value);
        }
    }
}
