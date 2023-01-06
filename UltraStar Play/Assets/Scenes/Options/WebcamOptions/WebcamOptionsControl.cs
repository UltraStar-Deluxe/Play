using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using Serilog.Core;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class WebcamOptionsControl : MonoBehaviour, INeedInjection, ITranslator
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.deviceContainer)]
    private VisualElement deviceContainer;

    [Inject(UxmlName = R.UxmlNames.webcamRenderContainer)]
    private Image webcamRenderContainer;

    [Inject(UxmlName = R.UxmlNames.useWebcamContainer)]
    private VisualElement useWebcamContainer;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject]
    private Settings settings;

    private LabeledItemPickerControl<WebCamDevice> devicePickerControl;
    private WebCamTexture webcamTexture;

    private void Start()
    {
        InitWebcamPicker();

        backButton.RegisterCallbackButtonTriggered(NavigateBack);
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5).Subscribe(_ => NavigateBack());
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        useWebcamContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_webcam_useAsBackGroundInSingingScene);
        useWebcamContainer.Q<Toggle>().value = settings.WebcamSettings.UseAsBackgroundInSingScene;
        useWebcamContainer.Q<Toggle>().RegisterValueChangedCallback(
            (changeEvent) => 
                settings.WebcamSettings.UseAsBackgroundInSingScene = changeEvent.newValue
            );
        deviceContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_webcam_device);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options_webcam_title);
    }

    private void InitWebcamPicker()
    {
        devicePickerControl = new LabeledItemPickerControl<WebCamDevice>(deviceContainer.Q<ItemPicker>(), WebCamTexture.devices.ToList());
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
                    if (webcamTexture != null && webcamTexture.isPlaying)
                    {
                        webcamTexture.Stop();
                    }

                    webcamTexture = new WebCamTexture(device.name);
                    Log.Logger.Information("Setting current webcam to '{webcamname}'", webcamTexture.deviceName);
                    webcamTexture.Play();

                    webcamRenderContainer.image = webcamTexture;
                });
        }
        else
        {
            Log.Logger.Information("No webcam found");
            devicePickerControl.GetLabelTextFunction = nullDevice => TranslationManager.GetTranslation(R.Messages.options_webcam_noWebcamsAvailable);
            devicePickerControl.Items.Add(new WebCamDevice());
            useWebcamContainer.SetEnabled(false);
        }
    }

    private void NavigateBack()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
        sceneNavigator.LoadScene(EScene.OptionsScene);
    }

    private bool TryReSelectLastWebcam()
    {
        if (settings.WebcamSettings.CurrentDeviceName.IsNullOrEmpty())
        {
            return false;
        }

        WebCamDevice lastDevice = devicePickerControl.Items
            .FirstOrDefault(device => device.name == settings.WebcamSettings.CurrentDeviceName);

        devicePickerControl.SelectItem(lastDevice);
        return true;
    }
}
