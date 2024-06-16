using System.Collections.Generic;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CreditsSceneControl : MonoBehaviour, INeedInjection, IBinder
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    private void Start()
    {
        backButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.MainScene));
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.MainScene));
        backButton.Focus();
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        return bb.GetBindings();
    }
}
