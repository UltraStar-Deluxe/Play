using System;

public class LazyFromContext<CONTEXT, T>
{
    private T value;
    
    public bool IsValueLoaded { get; private set; }

    private readonly Func<CONTEXT, T> valueProvider;

    public LazyFromContext(Func<CONTEXT, T> valueProvider)
    {
        this.valueProvider = valueProvider;
    }

    public T GetValue(CONTEXT context)
    {
        if (!IsValueLoaded)
        {
            value = valueProvider.Invoke(context);
            IsValueLoaded = true;
        }

        return value;
    }
}
