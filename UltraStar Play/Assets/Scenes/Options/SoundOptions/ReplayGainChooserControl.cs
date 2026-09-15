using System;
using System.Collections.Generic;

public class ReplayGainChooserControl : EnumChooserControl<EReplayGainMode>
{
    private const string VlcOptionName = "--audio-replay-gain-mode";

    public ReplayGainChooserControl(Chooser chooser)
        : base(chooser, EnumUtils.GetValuesAsList<EReplayGainMode>())
    {
    }

    public static void SetReplayGainEnumValue(Settings settings, EReplayGainMode newValue)
    {
        settings.ReplayGainMode = newValue;
        settings.VlcOptions.RemoveAll(line => line.Trim().StartsWith($"{VlcOptionName}="));

        if (newValue is EReplayGainMode.Track)
        {
            settings.VlcOptions.Add(GetVlcOption(EReplayGainMode.Track));
        }
        else if (newValue is EReplayGainMode.Album)
        {
            settings.VlcOptions.Add(GetVlcOption(EReplayGainMode.Album));
        }
    }

    private static string GetVlcOption(EReplayGainMode value)
    {
        switch (value)
        {
            case EReplayGainMode.Off:
                return "";
            case EReplayGainMode.Track:
                return $"{VlcOptionName}=track";
            case EReplayGainMode.Album:
                return $"{VlcOptionName}=album";
            default:
                throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }
    }
}
