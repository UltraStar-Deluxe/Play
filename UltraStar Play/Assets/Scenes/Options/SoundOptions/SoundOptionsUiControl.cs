using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SoundOptionsUiControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject]
    private UIDocument uiDoc;

    [Inject(Key = R.UxmlNames.backgroundMusicChooserHashed)]
    private ItemPicker backgroundMusicChooser;

    [Inject]
    private Settings settings;

    private void Start()
    {
        uiDoc.rootVisualElement.Query<Button>().ForEach(button => button.focusable = true);

        new BoolItemPickerControl(backgroundMusicChooser)
            .Bind(() => settings.AudioSettings.BackgroundMusicEnabled,
                  newValue => settings.AudioSettings.BackgroundMusicEnabled = newValue);

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
    }
}
