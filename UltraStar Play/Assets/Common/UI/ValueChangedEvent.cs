public class ValueChangedEvent<T>
{
    public T OldValue { get; private set; }
    public T NewValue { get; private set; }

    public ValueChangedEvent(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
