using System;
using UniInject;

public class MicProgressBarControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R_PlayShared.UxmlNames.micProgressBar)]
    private RadialProgressBar micProgressBar;

    [Inject(Optional = true)]
    private MicProfile micProfile;
    public MicProfile MicProfile
    {
        get => micProfile;
        set
        {
            micProfile = value;
            UpdateMicProgressBar();
        }
    }

    public Action<MicProfile> OnProgressBarFilled { get; set; }

    public float ProgressBarValue
    {
        get => micProgressBar.ProgressInPercent;
        set
        {
            micProgressBar.ProgressInPercent = value;
            micProgressBar.SetVisibleByDisplay(value > 0);

            if (value >= micProgressBar.highValue)
            {
                OnProgressBarFilled?.Invoke(MicProfile);
            }
        }
    }

    public void OnInjectionFinished()
    {
        micProgressBar.HideByDisplay();
        UpdateMicProgressBar();
    }

    private void UpdateMicProgressBar()
    {
        if (MicProfile == null)
        {
            micProgressBar.ProgressInPercent = 0;
            micProgressBar.HideByDisplay();
            return;
        }
        micProgressBar.ProgressColor = MicProfile.Color;
    }
}
