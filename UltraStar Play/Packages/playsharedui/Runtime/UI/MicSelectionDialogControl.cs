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
        AddButton(TranslationManager.GetTranslation("cancel"), _ => CloseDialog());

        // Use mic sample recorders in scene to select mic by singing
    }

    private void UpdateMicProfileList()
    {
        dialogMessageContainer.Clear();
        micWithNameControls.ForEach(it => it.Dispose());
        micWithNameControls.Clear();

        micProfiles.ForEach(otherMicProfile =>
        {
            VisualElement micWithName = micWithNameUi.CloneTreeAndGetFirstChild();
            AddVisualElement(micWithName);

            MicWithNameControl micWithNameControl = injector
                .WithRootVisualElement(micWithName)
                .WithBindingForInstance(otherMicProfile)
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

    public override void CloseDialog()
    {
        micWithNameControls.ForEach(it => it.Dispose());
        micWithNameControls.Clear();
        base.CloseDialog();
    }

    public class MicProfileChangedEvent
    {
        public string playerName;
        public MicProfile oldMicProfile;
        public MicProfile newMicProfile;
    }
}
