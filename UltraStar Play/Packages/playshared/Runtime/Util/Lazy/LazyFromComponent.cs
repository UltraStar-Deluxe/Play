using System;
using UnityEngine;

public class LazyFromComponent<T> : LazyFromContext<Component, T>
{
    public LazyFromComponent(Func<Component, T> valueProvider)
        : base(valueProvider)
    {
    }
}
