using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UnityEngine;
using UniInject;
using UniRx;
using ProTrans;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class RecordingOptionsSceneUiControl : MonoBehaviour, INeedInjection, ITranslator
{
    private static readonly List<int> amplificationItems = new List<int> { 0, 3, 6, 9, 12, 15, 18 };
    private static readonly List<int> noiseSuppressionItems= new List<int> { 0, 5, 10, 15, 20, 25, 30 };

    [Inject]
    private Settings settings;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.deviceContainer)]
    private VisualElement deviceContainer;

    [Inject(UxmlName = R.UxmlNames.amplificationContainer)]
    private VisualElement amplificationContainer;

    [Inject(UxmlName = R.UxmlNames.noiseSuppressionContainer)]
    private VisualElement noiseSuppressionContainer;

    [Inject(UxmlName = R.UxmlNames.delayContainer)]
    private VisualElement delayContainer;

    [Inject(UxmlName = R.UxmlNames.colorContainer)]
    private VisualElement colorContainer;

    [Inject(UxmlName = R.UxmlNames.enabledToggle)]
    private Toggle enabledToggle;

    [Inject(UxmlName = R.UxmlNames.enabledLabel)]
    private VisualElement enabledLabel;

    [Inject(UxmlName = R.UxmlNames.notConnectedContainer)]
    private VisualElement notConnectedContainer;

    [Inject(UxmlName = R.UxmlNames.notConnectedLabel)]
    private VisualElement notConnectedLabel;

    [Inject(UxmlName = R.UxmlNames.deleteButton)]
    private Button deleteButton;

    [Inject(UxmlName = R.UxmlNames.micVisualization)]
    private VisualElement micVisualization;

    // public RecordingOptionsMicVisualizer micVisualizer;

    private RecordingDevicePickerControl devicePickerControl;
    private LabeledItemPickerControl<int> amplificationPickerControl;
    private LabeledItemPickerControl<int> noiseSuppressionPickerControl;
    private NumberPickerControl delayPickerControl;
    private ColorPickerControl colorPickerControl;

    private MicProfile SelectedMicProfile => devicePickerControl.SelectedItem;

    void Start()
    {
        devicePickerControl = new RecordingDevicePickerControl(deviceContainer.Q<ItemPicker>(), CreateMicProfiles());
        devicePickerControl.Selection.Value = devicePickerControl.Items[0];
        amplificationPickerControl = new LabeledItemPickerControl<int>(amplificationContainer.Q<ItemPicker>(), amplificationItems);
        noiseSuppressionPickerControl = new LabeledItemPickerControl<int>(noiseSuppressionContainer.Q<ItemPicker>(), noiseSuppressionItems);
        delayPickerControl = new NumberPickerControl(delayContainer.Q<ItemPicker>());
        colorPickerControl = new ColorPickerControl(colorContainer.Q<ItemPicker>(), GetColorItems());
        enabledToggle.RegisterValueChangedCallback(evt => SetSelectedRecordingDeviceEnabled(evt.newValue));
        deleteButton.RegisterCallbackButtonTriggered(() => DeleteSelectedRecordingDevice());

        devicePickerControl.Selection.Subscribe(newValue => OnRecordingDeviceSelected(newValue));
        amplificationPickerControl.Selection.Subscribe(newValue => SelectedMicProfile.Amplification = newValue);
        noiseSuppressionPickerControl.Selection.Subscribe(newValue => SelectedMicProfile.NoiseSuppression = newValue);
        delayPickerControl.Selection.Subscribe(newValue => SelectedMicProfile.DelayInMillis = (int)newValue);
        colorPickerControl.Selection.Subscribe(newValue => SelectedMicProfile.Color = newValue);

        // Reselect recording device of connected client, when the client has now connected
        serverSideConnectRequestManager.ClientConnectedEventStream
            .Where(clientConnectedEvent => devicePickerControl.SelectedItem?.ConnectedClientId == clientConnectedEvent.ConnectedClientHandler.ClientId)
            .Subscribe(newValue => OnRecordingDeviceSelected(devicePickerControl.SelectedItem))
            .AddTo(gameObject);

        serverSideConnectRequestManager.ClientConnectedEventStream
            .Subscribe(UpdateMicProfileNames)
            .AddTo(gameObject);

        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsScene));
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
    }

    private void SetSelectedRecordingDeviceEnabled(bool isEnabled)
    {
        if (SelectedMicProfile == null)
        {
            return;
        }
        SelectedMicProfile.IsEnabled = isEnabled;
        if (isEnabled)
        {
            settings.MicProfiles.AddIfNotContains(SelectedMicProfile);
        }
    }

    private void OnRecordingDeviceSelected(MicProfile micProfile)
    {
        if (micProfile == null)
        {
            return;
        }

        amplificationPickerControl.Selection.Value = amplificationPickerControl.Items
            .FirstOrDefault(it => it == micProfile.Amplification)
            .OrIfNull(amplificationItems[0]);

        noiseSuppressionPickerControl.Selection.Value = noiseSuppressionPickerControl.Items
            .FirstOrDefault(it => it == micProfile.NoiseSuppression)
            .OrIfNull(noiseSuppressionItems[0]);

        delayPickerControl.Selection.Value = micProfile.DelayInMillis;

        colorPickerControl.Selection.Value = colorPickerControl.Items
            .FirstOrDefault(it => it.ColorEquals(micProfile.Color))
            .OrIfNull(GetColorItems()[0]);

        enabledToggle.value = micProfile.IsEnabled;

        if (micProfile.IsConnected)
        {
            notConnectedContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            deleteButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }
        else
        {
            notConnectedContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            deleteButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }

        // micVisualizer.SetMicProfile(micProfile);
    }

    private void DeleteSelectedRecordingDevice()
    {
        if (SelectedMicProfile == null)
        {
            return;
        }

        if (!SelectedMicProfile.IsConnected)
        {
            settings.MicProfiles.Remove(SelectedMicProfile);
            devicePickerControl.Items = CreateMicProfiles();
            if (devicePickerControl.Items.Count > 0)
            {
                devicePickerControl.Selection.Value = devicePickerControl.Items[0];
            }
        }
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        // sceneTitle.text = TranslationManager.GetTranslation(R.Messages.recordingOptionsScene_title);
    }

    private List<MicProfile> CreateMicProfiles()
    {
        // Create list of connected and loaded microphones without duplicates.
        // A loaded microphone might have been created with hardware that is not connected now.
        List<string> connectedMicNames = Microphone.devices.ToList();
        List<MicProfile> loadedMicProfiles = settings.MicProfiles;
        List<MicProfile> micProfiles = new List<MicProfile>(loadedMicProfiles);
        List<ConnectedClientHandler> connectedClientHandlers = ServerSideConnectRequestManager.GetConnectedClientHandlers();

        // Create mic profiles for connected microphones that are not yet in the list
        foreach (string connectedMicName in connectedMicNames)
        {
            bool alreadyInList = micProfiles.AnyMatch(it => it.Name == connectedMicName && !it.IsInputFromConnectedClient);
            if (!alreadyInList)
            {
                MicProfile micProfile = new MicProfile(connectedMicName);
                micProfiles.Add(micProfile);
            }
        }

        // Create mic profiles for connected companion apps that are not yet in the list
        foreach (ConnectedClientHandler connectedClientHandler in connectedClientHandlers)
        {
            bool alreadyInList = micProfiles.AnyMatch(it => it.ConnectedClientId == connectedClientHandler.ClientId && it.IsInputFromConnectedClient);
            if (!alreadyInList)
            {
                MicProfile micProfile = new MicProfile(connectedClientHandler.ClientName, connectedClientHandler.ClientId);
                micProfiles.Add(micProfile);
            }
        }

        return micProfiles;
    }

    public void UpdateMicProfileNames(ClientConnectionEvent clientConnectionEvent)
    {
        if (clientConnectionEvent.IsConnected)
        {
            devicePickerControl.Items.ForEach(micProfile => UpdateMicProfileName(clientConnectionEvent, micProfile));
        }
        devicePickerControl.UpdateLabelText();
    }

    private void UpdateMicProfileName(ClientConnectionEvent clientConnectionEvent, MicProfile micProfile)
    {
        if (micProfile.IsInputFromConnectedClient
            && micProfile.ConnectedClientId == clientConnectionEvent.ConnectedClientHandler.ClientId)
        {
            micProfile.Name = clientConnectionEvent.ConnectedClientHandler.ClientName;

            if (devicePickerControl.SelectedItem == micProfile)
            {
                devicePickerControl.UpdateLabelText();
            }
        }
    }

    private List<Color32> GetColorItems()
    {
        return new List<Color32>
        {
            ThemeManager.GetColor(R.Color.deviceColor_1),
            ThemeManager.GetColor(R.Color.deviceColor_2),
            ThemeManager.GetColor(R.Color.deviceColor_3),
            ThemeManager.GetColor(R.Color.deviceColor_4),
            ThemeManager.GetColor(R.Color.deviceColor_5),
            ThemeManager.GetColor(R.Color.deviceColor_6)
        };
    }
}
