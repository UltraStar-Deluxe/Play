using System;
using UniInject;
using UnityEngine.UIElements;

public class MicWithNameControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    [Inject(UxmlName = R_PlayShared.UxmlNames.micButton)]
    private Button micButton;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.nameLabel)]
    private Label nameLabel;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.micIcon)]
    private VisualElement micIcon;

    [Inject]
    public MicProfile MicProfile { get; set; }

    [Inject]
    private Injector injector;

    public MicProgressBarRecordingControl MicProgressBarRecordingControl { get; private set; } = new();
    
    public Action<MicProfile> OnMicSelected { get; set; }

    public void OnInjectionFinished()
    {
        micButton.RegisterCallbackButtonTriggered(_ => OnMicSelected?.Invoke(MicProfile));
        nameLabel.text = MicProfile.GetDisplayNameWithChannel();
        nameLabel.RegisterCallback<ClickEvent>(evt => OnMicSelected?.Invoke(MicProfile));
        micIcon.style.unityBackgroundImageTintColor = new StyleColor(MicProfile.Color);
        micIcon.style.color = new StyleColor(MicProfile.Color);
        
        injector.Inject(MicProgressBarRecordingControl);
        MicProgressBarRecordingControl.MicProgressBarControl.OnProgressBarFilled = localMicProfile => OnMicSelected?.Invoke(localMicProfile);
    }

    public void Update()
    {
        MicProgressBarRecordingControl.Update();
    }

    public void Dispose()
    {
        MicProgressBarRecordingControl?.Dispose();
    }
}
