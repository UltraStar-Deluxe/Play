using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using ProTrans;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneWebcamControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private Settings settings;

    [Inject(UxmlName = R.UxmlNames.webcamRenderContainer)]
    private Image webcamRenderContainer;

    private WebCamTexture webcamTexture;

    public void InitWebcam()
    {
        webcamTexture = new WebCamTexture(settings.WebcamSettings.CurrentDeviceName);
        webcamRenderContainer.image = webcamTexture;
        if (WebcamsAvailable())
        {
            if (settings.WebcamSettings.UseAsBackgroundInSingScene)
            {
                webcamTexture.Play();
            }

            webcamRenderContainer.SetVisibleByDisplay(settings.WebcamSettings.UseAsBackgroundInSingScene);
        }
    }

    public void Play()
    {
        webcamTexture.Play();
    }

    public void Stop()
    {
        webcamTexture.Stop();
    }

    public string CurrentDeviceName()
    {
        return webcamTexture.deviceName;
    }

    public bool WebcamsAvailable()
    {
        return WebCamTexture.devices.Length > 0;
    }

    public void AddToContextMenu(ContextMenuPopupControl contextMenuPopup)
    {
        if (WebcamsAvailable())
        {
            contextMenuPopup.AddItem(TranslationManager.GetTranslation(R.Messages.action_webcamOnOff),
                () =>
                {
                    bool displayWebcam = !webcamRenderContainer.IsVisibleByDisplay();
                    if (displayWebcam)
                    {
                        Log.Logger.Information("Webcam activated: {webcamname}", webcamTexture.deviceName);
                        Play();
                    }
                    else
                    {
                        Log.Logger.Information("Webcam deactivated: {webcamname}", webcamTexture.deviceName);
                        Stop();
                    }

                    webcamRenderContainer.SetVisibleByDisplay(displayWebcam);
                    settings.WebcamSettings.UseAsBackgroundInSingScene = displayWebcam;
                });
        }
    }
}
