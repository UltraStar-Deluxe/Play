using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class WebcamOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject(UxmlName = R.UxmlNames.deviceContainer)]
    private VisualElement deviceContainer;

    [Inject(UxmlName = R.UxmlNames.webcamRenderContainer)]
    private Image webcamRenderContainer;

    [Inject(UxmlName = R.UxmlNames.useWebcamContainer)]
    private VisualElement useWebcamContainer;

    [Inject]
    private Settings settings;

    [Inject]
    private WebCamManager webCamManager;

    private LabeledItemPickerControl<WebCamDevice> devicePickerControl;

    private void Start()
    {
        InitWebcamPicker();
    }

    public void UpdateTranslation()
    {
        useWebcamContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_webcam_useAsBackGroundInSingingScene);
        useWebcamContainer.Q<Toggle>().value = settings.WebcamSettings.UseAsBackgroundInSingScene;
        useWebcamContainer.Q<Toggle>().RegisterValueChangedCallback(
            (changeEvent) => 
                settings.WebcamSettings.UseAsBackgroundInSingScene = changeEvent.newValue
            );
        deviceContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_webcam_device);
    }

    private void InitWebcamPicker()
    {
        devicePickerControl = new LabeledItemPickerControl<WebCamDevice>(deviceContainer.Q<ItemPicker>(), webCamManager.GetWebCamDevices());
        devicePickerControl.GetLabelTextFunction = device => device.name;
        if (!TryReSelectLastWebcam() && devicePickerControl.Items.Count > 0)
        {
            devicePickerControl.Selection.Value = devicePickerControl.Items[0];
        }
        if (devicePickerControl.Items.Count > 0)
        {
            devicePickerControl.Selection
                .Subscribe(device =>
                {
                    settings.WebcamSettings.CurrentDeviceName = device.name;
                    WebCamTexture webCamTexture = webCamManager.StartSelectedWebCam();
                    webcamRenderContainer.image = webCamTexture;
                });
        }
        else
        {
            Debug.Log("No webcam found");
            devicePickerControl.GetLabelTextFunction = nullDevice => TranslationManager.GetTranslation(R.Messages.options_webcam_noWebcamsAvailable);
            devicePickerControl.Items.Add(new WebCamDevice());
            useWebcamContainer.SetEnabled(false);
        }
    }

    private bool TryReSelectLastWebcam()
    {
        if (settings.WebcamSettings.CurrentDeviceName.IsNullOrEmpty())
        {
            return false;
        }

        WebCamDevice lastSelectedDevice = devicePickerControl.Items
            .FirstOrDefault(device => device.name == settings.WebcamSettings.CurrentDeviceName);

        devicePickerControl.SelectItem(lastSelectedDevice);
        return true;
    }
}
