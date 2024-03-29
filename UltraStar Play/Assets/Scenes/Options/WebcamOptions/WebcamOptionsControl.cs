using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class WebcamOptionsControl : AbstractOptionsSceneControl, INeedInjection
{
    [Inject(UxmlName = R.UxmlNames.devicePicker)]
    private ItemPicker devicePicker;

    [Inject(UxmlName = R.UxmlNames.webcamRenderContainer)]
    private Image webcamRenderContainer;

    [Inject(UxmlName = R.UxmlNames.useWebcamToggle)]
    private Toggle useWebcamToggle;

    [Inject]
    private WebCamManager webCamManager;

    private LabeledItemPickerControl<WebCamDevice> devicePickerControl;

    protected override void Start()
    {
        base.Start();

        InitWebcamPicker();
    }

    public void UpdateTranslation()
    {
        useWebcamToggle.label = Translation.Get(R.Messages.options_webcam_useAsBackGroundInSingingScene);
        useWebcamToggle.value = settings.UseWebcamAsBackgroundInSingScene;
        useWebcamToggle.RegisterValueChangedCallback(evt => settings.UseWebcamAsBackgroundInSingScene = evt.newValue);
        devicePicker.Label = Translation.Get(R.Messages.options_webcam_device);
    }

    private void InitWebcamPicker()
    {
        devicePickerControl = new LabeledItemPickerControl<WebCamDevice>(devicePicker, webCamManager.GetWebCamDevices());
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
                    settings.CurrentWebcamDeviceName = device.name;
                    WebCamTexture webCamTexture = webCamManager.StartSelectedWebCam();
                    webcamRenderContainer.image = webCamTexture;
                });
        }
        else
        {
            Debug.Log("No webcam found");
            devicePickerControl.GetLabelTextFunction = nullDevice => Translation.Get(R.Messages.options_webcam_noWebcamsAvailable);
            devicePickerControl.Items.Add(new WebCamDevice());
        }
    }

    private bool TryReSelectLastWebcam()
    {
        if (settings.CurrentWebcamDeviceName.IsNullOrEmpty())
        {
            return false;
        }

        WebCamDevice lastSelectedDevice = devicePickerControl.Items
            .FirstOrDefault(device => device.name == settings.CurrentWebcamDeviceName);

        devicePickerControl.SelectItem(lastSelectedDevice);
        return true;
    }
}
