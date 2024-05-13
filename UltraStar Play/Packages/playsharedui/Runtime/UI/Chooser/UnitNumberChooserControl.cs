public class UnitNumberChooserControl : NumberChooserControl
{
    public UnitNumberChooserControl(Chooser chooser, string unit, double initialValue = 0)
        : base(chooser, initialValue)
    {
        GetLabelTextFunction = item => item + $" {unit}";
    }
}
