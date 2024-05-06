

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PitchDetectionAlgorithmPickerControl : LabeledItemPickerControl<EPitchDetectionAlgorithm>
{
    public PitchDetectionAlgorithmPickerControl(ItemPicker itemPicker)
        : base(itemPicker,
            EnumUtils.GetValuesAsList<EPitchDetectionAlgorithm>(),
            item =>
            {
                switch (item)
                {
                    case EPitchDetectionAlgorithm.Dywa:
                        return Translation.Of("Dynamic Wavelet\n(default)");
                    case EPitchDetectionAlgorithm.Camd:
                        return Translation.Of("Circular Average\nMagnitude Difference");
                    default:
                        return Translation.Of(item.ToString());
                }
            })
    {
    }
}
