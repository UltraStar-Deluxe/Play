using System;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class MicWithNameControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R_PlayShared.UxmlNames.micButton)]
    private Button micButton;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.nameLabel)]
    private Label nameLabel;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.micIcon)]
    private VisualElement micIcon;

    [Inject(UxmlName = R_PlayShared.UxmlNames.micProgressBar)]
    private RadialProgressBar micProgressBar;
    
    [Inject]
    public MicProfile MicProfile { get; set; }

    public Action<MicProfile> OnMicSelected { get; set; }

    public float ProgressBarValue
    {
        get => micProgressBar.value;
        set
        {
            micProgressBar.value = value;
            micProgressBar.SetVisibleByDisplay(value > 0);

            if (value >= micProgressBar.highValue)
            {
                OnMicSelected?.Invoke(MicProfile);
            }
        }
    }

    public void OnInjectionFinished()
    {
        micButton.RegisterCallbackButtonTriggered(() => OnMicSelected?.Invoke(MicProfile));
        nameLabel.text = MicProfile.Name;
        nameLabel.RegisterCallback<ClickEvent>(evt => OnMicSelected?.Invoke(MicProfile));
        micIcon.style.unityBackgroundImageTintColor = new StyleColor(MicProfile.Color);
        micIcon.style.unityBackgroundImageTintColor = new StyleColor(MicProfile.Color);
        micProgressBar.HideByDisplay();
        micProgressBar.progressColor = MicProfile.Color;
    }
}
