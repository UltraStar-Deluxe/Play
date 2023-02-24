using System;
using UniInject;

public class MicProgressBarControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R_PlayShared.UxmlNames.micProgressBar)]
    private RadialProgressBar micProgressBar;

    [Inject]
    public MicProfile MicProfile { get; set; }

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
        micProgressBar.progressColor = MicProfile.Color;
    }
}
