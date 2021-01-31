using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(Button))]
public class ChangeSceneButton : MonoBehaviour, INeedInjection
{
    public EScene targetScene;
    public bool triggerWithEscape;

    [Inject]
    private InputActions inputActions;
    
    void Start()
    {
        GetComponent<Button>().OnClickAsObservable()
            .Subscribe(_ => SceneNavigator.Instance.LoadScene(targetScene));

        if (triggerWithEscape)
        {
            inputActions.USPlay.BackAction.PerformedAsObservable().Subscribe(OnBack);
        }
    }

    private void OnBack(InputAction.CallbackContext context)
    {
        if (!UiManager.Instance.DialogOpen)
        {
            SceneNavigator.Instance.LoadScene(targetScene);
        }
    }
}
