public abstract class ComputedChooserControl<T> : AbstractChooserControl<T>
{
    protected ComputedChooserControl(Chooser chooser, T initialValue)
        : base(chooser)
    {
        Selection = initialValue;
    }
}
