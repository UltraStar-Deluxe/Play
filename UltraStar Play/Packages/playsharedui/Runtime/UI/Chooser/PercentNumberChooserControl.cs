public class PercentNumberChooserControl : UnitNumberChooserControl
{
    public PercentNumberChooserControl(Chooser chooser, double initialValue = 0)
        : base(chooser, "%", initialValue)
    {
    }
}
