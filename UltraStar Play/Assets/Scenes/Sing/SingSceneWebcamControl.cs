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
        webcamTexture = new WebCamTexture(settings.CurrentWebcamDeviceName);
        webcamRenderContainer.image = webcamTexture;
        if (WebcamsAvailable())
        {
            if (settings.UseWebcamAsBackgroundInSingScene)
            {
                webcamTexture.Play();
            }

            webcamRenderContainer.SetVisibleByDisplay(settings.UseWebcamAsBackgroundInSingScene);
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

    public void SetUseAsBackgroundInSingScene(bool newValue)
    {
        if (settings.UseWebcamAsBackgroundInSingScene == newValue)
        {
            return;
        }

        settings.UseWebcamAsBackgroundInSingScene = newValue;
        if (newValue)
        {
            Play();
            Log.Logger.Information("Webcam activated: {webcamname}", webcamTexture.deviceName);
            webcamRenderContainer.ShowByDisplay();
        }
        else
        {
            Stop();
            Log.Logger.Information("Webcam deactivated: {webcamname}", webcamTexture.deviceName);
            webcamRenderContainer.HideByDisplay();
        }
    }
    
    public void ToggleUseAsBackgroundInSingScene()
    {
        SetUseAsBackgroundInSingScene(!settings.UseWebcamAsBackgroundInSingScene);
    }
}
