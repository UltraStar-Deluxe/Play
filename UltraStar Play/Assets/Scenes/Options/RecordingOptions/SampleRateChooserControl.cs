using System;
using System.Collections.Generic;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SampleRateChooserControl : LabeledChooserControl<int>
{
    public SampleRateChooserControl(Chooser chooser)
        : base(chooser,
            new List<int>{0, 48000, 44100, 22050, 16000 },
            item => item <= 0
                ? Translation.Get(R.Messages.options_sampleRate_auto)
                : Translation.Of(item.ToString()))
    {
    }

    public SampleRateChooserControl(Chooser chooser, Func<int, Translation> getLabelTextFunction)
        : base(chooser,
            new List<int>{0, 48000, 44100, 22050, 16000 },
            getLabelTextFunction)
    {
    }
}
