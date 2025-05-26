using System;
using System.Collections.Generic;

public class ReplayGainChooserControl : EnumChooserControl<EReplayGainLoudnessNormalization>
{
    private const string VlcOptionName = "--audio-replay-gain-mode";

    public ReplayGainChooserControl(Chooser chooser)
        : base(chooser, EnumUtils.GetValuesAsList<EReplayGainLoudnessNormalization>())
    {
    }

    public static EReplayGainLoudnessNormalization GetReplayGainEnumValue(List<string> vlcOptions)
    {
        if (vlcOptions.Contains(ReplayGainChooserControl.GetVlcOption(EReplayGainLoudnessNormalization.Track)))
        {
            return EReplayGainLoudnessNormalization.Track;
        }
        else if (vlcOptions.Contains(ReplayGainChooserControl.GetVlcOption(EReplayGainLoudnessNormalization.Album)))
        {
            return EReplayGainLoudnessNormalization.Album;
        }
        else
        {
            return EReplayGainLoudnessNormalization.Disabled;
        }
    }

    public static void SetReplayGainEnumValue(List<string> vlcOptions, EReplayGainLoudnessNormalization newValue)
    {
        vlcOptions.RemoveAll(line => line.Trim().StartsWith($"{VlcOptionName}="));

        if (newValue is EReplayGainLoudnessNormalization.Track)
        {
            vlcOptions.Add(GetVlcOption(EReplayGainLoudnessNormalization.Track));
        }
        else if (newValue is EReplayGainLoudnessNormalization.Album)
        {
            vlcOptions.Add(GetVlcOption(EReplayGainLoudnessNormalization.Album));
        }
    }

    private static string GetVlcOption(EReplayGainLoudnessNormalization value)
    {
        switch (value)
        {
            case EReplayGainLoudnessNormalization.Disabled:
                return "";
            case EReplayGainLoudnessNormalization.Track:
                return $"{VlcOptionName}=track";
            case EReplayGainLoudnessNormalization.Album:
                return $"{VlcOptionName}=album";
            default:
                throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }
    }
}
