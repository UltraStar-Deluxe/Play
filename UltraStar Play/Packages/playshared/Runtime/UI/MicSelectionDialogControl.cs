using System;
using System.Collections.Generic;
using ProTrans;
using UniInject;
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

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        AddButton(TranslationManager.GetTranslation("cancel"), () => CloseDialog());
    }

    private void UpdateMicProfileList()
    {
        dialogMessageContainer.Clear();
        micProfiles.ForEach(otherMicProfile =>
        {
            VisualElement micWithName = micWithNameUi.CloneTreeAndGetFirstChild();
            micWithName.Q<Button>(R_PlayShared.UxmlNames.micButton).RegisterCallbackButtonTriggered(() => OnMicSelected(otherMicProfile));
            micWithName.Q<Label>(R_PlayShared.UxmlNames.nameLabel).text = otherMicProfile.Name;
            micWithName.Q<Label>(R_PlayShared.UxmlNames.nameLabel).RegisterCallback<ClickEvent>(evt => OnMicSelected(otherMicProfile));
            micWithName.Q<VisualElement>(R_PlayShared.UxmlNames.micIcon).style.unityBackgroundImageTintColor = new StyleColor(otherMicProfile.Color);

            AddVisualElement(micWithName);
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
