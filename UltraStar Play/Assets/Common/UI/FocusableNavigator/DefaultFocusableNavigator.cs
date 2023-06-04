using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DefaultFocusableNavigator : FocusableNavigator
{
    public static DefaultFocusableNavigator Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<DefaultFocusableNavigator>();
    
	public override void OnInjectionFinished() {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        base.OnInjectionFinished();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(100)
            .Subscribe(_ => OnBack());
        InputManager.GetInputAction(R.InputActions.ui_submit).PerformedAsObservable()
            .Subscribe(_ => OnSubmit());
        InputManager.GetInputAction(R.InputActions.ui_navigate).PerformedAsObservable()
            .Subscribe(context => OnNavigate(context.ReadValue<Vector2>(), context.action, context.control));
	}

    protected override VisualElement GetFocusableNavigatorRootVisualElement(VisualElement visualElement)
    {
        if (visualElement == uiDocument.rootVisualElement)
        {
            return visualElement;
        }

        return base.GetFocusableNavigatorRootVisualElement(visualElement);
    }
}
