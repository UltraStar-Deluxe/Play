using System;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class MicSelectionDialogControl : MessageDialogControl, INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(micWithNameUi))]
    private VisualTreeAsset micWithNameUi;

    private List<MicProfile> micProfiles;
    public List<MicProfile> MicProfiles
    {
        get => micProfiles;
        set
        {
            micProfiles = value;
            UpdateMicProfileList();
        }
    }

    public Action<MicProfile> OnMicProfileSelected { get; set; }

    private readonly List<MicWithNameControl> micWithNameControls = new();
    
    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        AddButton(TranslationManager.GetTranslation("cancel"), () => CloseDialog());

        // Use mic sample recorders in scene to select mic by singing
    }

    private void UpdateMicProfileList()
    {
        dialogMessageContainer.Clear();
        micWithNameControls.Clear();

        List<MicSampleRecorder> micSampleRecorders = GameObject.FindObjectsOfType<MicSampleRecorder>().ToList();

        micProfiles.ForEach(otherMicProfile =>
        {
            VisualElement micWithName = micWithNameUi.CloneTreeAndGetFirstChild();
            AddVisualElement(micWithName);

            MicSampleRecorder micSampleRecorder = micSampleRecorders.FirstOrDefault(micSampleRecorder => micSampleRecorder.MicProfile == otherMicProfile);

            MicWithNameControl micWithNameControl = injector
                .WithRootVisualElement(micWithName)
                .WithBindingForInstance(otherMicProfile)
                .WithBindingForInstance(micSampleRecorder)
                .CreateAndInject<MicWithNameControl>();
            micWithNameControl.OnMicSelected = OnMicSelected;
            
            micWithNameControls.Add(micWithNameControl);
        });
    }
    
    private void OnMicSelected(MicProfile newMicProfile)
    {
        OnMicProfileSelected?.Invoke(newMicProfile);
        CloseDialog();
    }
    
    public class MicProfileChangedEvent
    {
        public string playerName;
        public MicProfile oldMicProfile;
        public MicProfile newMicProfile;
    }
}
