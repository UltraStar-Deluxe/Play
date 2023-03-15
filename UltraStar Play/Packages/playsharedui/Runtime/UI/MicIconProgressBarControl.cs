using System;
using UniInject;

public class MicProgressBarControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = "micProgressBar")]
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
        get => micProgressBar.value;
        set
        {
            micProgressBar.value = value;
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
            micProgressBar.HideByDisplay();
            return;
        }
        micProgressBar.progressColor = MicProfile.Color;
    }
}
