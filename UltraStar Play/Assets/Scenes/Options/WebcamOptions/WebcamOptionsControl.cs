using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class WebcamOptionsControl : AbstractOptionsSceneControl, INeedInjection
{
    [Inject(UxmlName = R.UxmlNames.deviceChooser)]
    private Chooser deviceChooser;

    [Inject(UxmlName = R.UxmlNames.webcamRenderContainer)]
    private Image webcamRenderContainer;

    [Inject(UxmlName = R.UxmlNames.useWebcamToggle)]
    private Toggle useWebcamToggle;

    [Inject]
    private WebCamManager webCamManager;

    private LabeledChooserControl<WebCamDevice> deviceChooserControl;

    protected override void Start()
    {
        base.Start();

        useWebcamToggle.value = settings.UseWebcamAsBackgroundInSingScene;
        useWebcamToggle.RegisterValueChangedCallback(evt => settings.UseWebcamAsBackgroundInSingScene = evt.newValue);

        InitWebcamChooser();
    }

    private void InitWebcamChooser()
    {
        List<WebCamDevice> webCamDevices = webCamManager.GetWebCamDevices();
        deviceChooserControl = new LabeledChooserControl<WebCamDevice>(deviceChooser, webCamDevices,
            device => webCamDevices.Count <= 0
                ? Translation.Get(R.Messages.options_webcam_noWebcamsAvailable)
                : Translation.Of(device.name));
        if (!TryReSelectLastWebcam() && deviceChooserControl.Items.Count > 0)
        {
            deviceChooserControl.Selection = deviceChooserControl.Items[0];
        }
        if (deviceChooserControl.Items.Count > 0)
        {
            deviceChooserControl.SelectionAsObservable
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
            deviceChooserControl.Items.Add(new WebCamDevice());
        }
    }

    private bool TryReSelectLastWebcam()
    {
        if (settings.CurrentWebcamDeviceName.IsNullOrEmpty())
        {
            return false;
        }

        WebCamDevice lastSelectedDevice = deviceChooserControl.Items
            .FirstOrDefault(device => device.name == settings.CurrentWebcamDeviceName);

        deviceChooserControl.Selection = lastSelectedDevice;
        return true;
    }
}
