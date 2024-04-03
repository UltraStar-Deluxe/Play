using System;
using System.Collections.Generic;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class AbstractOptionsSceneControl : MonoBehaviour, INeedInjection
{
    [Inject]
    protected SceneNavigator sceneNavigator;

    [Inject]
    protected Settings settings;

    [Inject]
    protected NonPersistentSettings nonPersistentSettings;

    [Inject(UxmlName = R.UxmlNames.helpIcon, Optional = true)]
    protected VisualElement helpIcon;

    protected readonly List<IDisposable> disposables = new();

	protected virtual void Start() {
        disposables.Add(InputManager.GetInputAction(R.InputActions.usplay_back)
            .PerformedAsObservable(5)
            .Subscribe(_ => OnBack()));
	}

    protected void OnBack()
    {
        if (TryGoBack())
        {
            InputManager.GetInputAction(R.InputActions.usplay_back).CancelNotifyForThisFrame();
        }
    }

    protected virtual bool TryGoBack()
    {
        return false;
    }

    protected virtual void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
        disposables.Clear();
    }

    public void HighlightHelpIcon()
    {
        if (HelpUri.IsNullOrEmpty()
            || helpIcon == null)
        {
            return;
        }

        AnimationUtils.HighlightIconWithBounce(gameObject, helpIcon);
    }

    public virtual string HelpUri => "";

    public virtual bool HasIssuesDialog => false;
    public virtual MessageDialogControl CreateIssuesDialogControl()
    {
        return null;
    }

    public virtual string SteamWorkshopUri => "";
}
