using UnityEngine;

public class MicProfileMessageDto : CompanionAppMessageDto
{
    public string Name { get; set; }
    public string HexColor { get; set; }
    public int Amplification { get; set; }
    public int NoiseSuppression { get; set; }
    public int DelayInMillis { get; set; }
    public int SampleRate { get; set; }

    public MicProfileMessageDto() : base(CompanionAppMessageType.MicProfile)
    {
    }

    public MicProfileMessageDto(MicProfile micProfile) : this()
    {
        if (micProfile == null)
        {
            return;
        }

        Name = micProfile.Name;
        HexColor = ColorUtility.ToHtmlStringRGB(micProfile.Color);
        Amplification = micProfile.Amplification;
        NoiseSuppression = micProfile.NoiseSuppression;
        DelayInMillis = micProfile.DelayInMillis;
        SampleRate = micProfile.SampleRate;
    }
}
