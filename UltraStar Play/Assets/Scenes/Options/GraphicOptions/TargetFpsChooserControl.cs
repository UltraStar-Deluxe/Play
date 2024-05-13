using System.Collections.Generic;

public class TargetFpsChooserControl : LabeledChooserControl<int>
{
    public TargetFpsChooserControl(Chooser chooser)
        : base(chooser,
            new List<int>(){ -1, 30, 60 },
            newValue => newValue <= 0
                ? Translation.Get(R.Messages.options_sampleRate_auto)
                : Translation.Of(newValue.ToString()))
    {
    }
}
