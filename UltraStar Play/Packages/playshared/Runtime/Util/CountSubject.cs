using System;
using System.Threading;
using UniRx;

/**
 * Subject that keeps track of the number of subscriptions.
 * See https://stackoverflow.com/questions/37458936/count-all-subscriptions-of-a-subject
 */
public class CountSubject<T> : ISubject<T>, IDisposable
{
    private readonly ISubject<T> baseSubject;
    private readonly IDisposable disposer = Disposable.Empty;
    private bool disposed;
    private int counter;
    
    private readonly Subject<int> countSubject = new Subject<int>();
    public IObservable<int> Count => countSubject;

    public CountSubject()
        : this(new Subject<T>())
    {
        // Need to clear up Subject we created
        disposer = (IDisposable) baseSubject;
    }

    public CountSubject(ISubject<T> baseSubject)
    {
        this.baseSubject = baseSubject;
    }

    public void OnCompleted()
    {
        baseSubject.OnCompleted();
    }

    public void OnError(Exception error)
    {
        baseSubject.OnError(error);
    }

    public void OnNext(T value)
    {
        baseSubject.OnNext(value);
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        Interlocked.Increment(ref counter);
        CompositeDisposable compositeDisposable = new CompositeDisposable(
            Disposable.Create(() => OnDisposeSubscriber()),
            baseSubject.Subscribe(observer));
        countSubject.OnNext(counter);
        return compositeDisposable;
    }

    private void OnDisposeSubscriber()
    {
        Interlocked.Decrement(ref counter);
        countSubject.OnNext(counter);
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                disposer.Dispose();
            }
            disposed = true;
        }
    }
}
