using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(Button))]
public class ChangeSceneButton : MonoBehaviour, INeedInjection
{
    public EScene targetScene;
    public bool triggerWithEscape;

    void Start()
    {
        GetComponent<Button>().OnClickAsObservable()
            .Subscribe(_ => SceneNavigator.Instance.LoadScene(targetScene));

        if (triggerWithEscape)
        {
            InputManager.GetInputAction(R.InputActions.usplay_back)
                .PerformedAsObservable().Subscribe(OnBack);
        }
    }

    private void OnBack(InputAction.CallbackContext context)
    {
        SceneNavigator.Instance.LoadScene(targetScene);
    }
}
