using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

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
        if (webcamTexture == null)
        {
            return;
        }
        webcamTexture.Play();
    }

    public void Stop()
    {
        if (webcamTexture == null)
        {
            return;
        }
        webcamTexture.Stop();
    }

    public string CurrentDeviceName()
    {
        if (webcamTexture == null)
        {
            return "";
        }
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
            Debug.Log("Webcam activated: {webcamTexture.deviceName}");
            webcamRenderContainer.ShowByDisplay();
        }
        else
        {
            Stop();
            Debug.Log($"Webcam deactivated: {webcamTexture.deviceName}");
            webcamRenderContainer.HideByDisplay();
        }
    }
    
    public void ToggleUseAsBackgroundInSingScene()
    {
        SetUseAsBackgroundInSingScene(!settings.UseWebcamAsBackgroundInSingScene);
    }
}
