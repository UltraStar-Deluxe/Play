using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectMicListControl : MonoBehaviour, INeedInjection, ITranslator
{
    [InjectedInInspector]
    public VisualTreeAsset listEntryUi;

    [InjectedInInspector]
    public MicPitchTracker micPitchTrackerPrefab;

    [Inject(UxmlName = R.UxmlNames.micScrollView)]
    private VisualElement micScrollView;

    [Inject(UxmlName = R.UxmlNames.noMicsFoundLabel)]
    private Label noMicsFoundLabel;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    private readonly List<SongSelectMicEntryControl> listEntryControls = new();
    public IReadOnlyList<SongSelectMicEntryControl> MicEntryControls => listEntryControls;

    private void Start()
    {
        micScrollView.Clear();
        UpdateListEntries();

        // Remove/add MicProfile when Client (dis)connects.
        serverSideConnectRequestManager.ClientConnectedEventStream
            .Subscribe(HandleClientConnectedEvent)
            .AddTo(gameObject);
    }

    private void Update()
    {
        listEntryControls.ForEach(entry => entry.UpdateWaveForm());
    }

    private void HandleClientConnectedEvent(ClientConnectionEvent connectionEvent)
    {
        // Find existing or create new MicProfile for the newly connected device
        MicProfile micProfile = settings.MicProfiles.FirstOrDefault(it => it.ConnectedClientId == connectionEvent.ConnectedClientHandler.ClientId);
        if (micProfile == null)
        {
            micProfile = new MicProfile(connectionEvent.ConnectedClientHandler.ClientName, connectionEvent.ConnectedClientHandler.ClientId);
            settings.MicProfiles.Add(micProfile);
        }

        SongSelectMicEntryControl matchingEntryControl = listEntryControls.FirstOrDefault(listEntry =>
               listEntry.MicProfile != null
            && listEntry.MicProfile.ConnectedClientId == connectionEvent.ConnectedClientHandler.ClientId
            && listEntry.MicProfile.IsEnabled);
        if (connectionEvent.IsConnected && matchingEntryControl == null && micProfile.IsEnabled)
        {
            // Add to UI
            CreateListEntry(micProfile);
            noMicsFoundLabel.HideByDisplay();
        }
        else if (!connectionEvent.IsConnected && matchingEntryControl != null)
        {
            // Remove from UI
            RemoveListEntry(matchingEntryControl);
        }
    }

    private void UpdateListEntries()
    {
        // Remove old entries
        new List<SongSelectMicEntryControl>(listEntryControls).ForEach(entry => RemoveListEntry(entry));

        // Create new entries
        List<MicProfile> micProfiles = settings.MicProfiles;
        List<MicProfile> enabledAndConnectedMicProfiles = micProfiles
            .Where(it => it.IsEnabled && it.IsConnected(serverSideConnectRequestManager))
            .ToList();
        if (enabledAndConnectedMicProfiles.IsNullOrEmpty())
        {
            noMicsFoundLabel.ShowByDisplay();
        }
        else
        {
            noMicsFoundLabel.HideByDisplay();
            foreach (MicProfile micProfile in enabledAndConnectedMicProfiles)
            {
                CreateListEntry(micProfile);
            }
        }
    }

    private void CreateListEntry(MicProfile micProfile)
    {
        VisualElement visualElement = listEntryUi.CloneTree().Children().FirstOrDefault();
        micScrollView.Add(visualElement);

        MicPitchTracker micPitchTracker = Instantiate(micPitchTrackerPrefab, gameObject.transform);
        injector.InjectAllComponentsInChildren(micPitchTracker);
        micPitchTracker.MicProfile = micProfile;
        if (!micProfile.IsInputFromConnectedClient)
        {
            micPitchTracker.MicSampleRecorder.StartRecording();
        }

        SongSelectMicEntryControl entryControl = new(gameObject, visualElement, micPitchTracker);
        injector.WithRootVisualElement(visualElement).Inject(entryControl);
        entryControl.MicProfile = micProfile;
        listEntryControls.Add(entryControl);
    }

    private void RemoveListEntry(SongSelectMicEntryControl entryControl)
    {
        entryControl.Destroy();
        listEntryControls.Remove(entryControl);
        if (listEntryControls.IsNullOrEmpty())
        {
            noMicsFoundLabel.ShowByDisplay();
        }
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && noMicsFoundLabel == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        noMicsFoundLabel.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_noMicsFound);
    }
}
