using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UniRx;
using UnityEngine;

public class QuestionDialog : Dialog
{
    public Button yesButton;
    public Button noButton;

    public Action yesAction;
    public Action noAction;

    public bool noOnEscape = true;
    public bool focusYesOnStart = true;

    private List<IDisposable> disposables = new List<IDisposable>();
    
    private void Start()
    {
        yesButton.OnClickAsObservable().Subscribe(_ =>
        {
            yesAction?.Invoke();
            Close();
        });
        noButton.OnClickAsObservable().Subscribe(_ =>
        {
            noAction?.Invoke();
            Close();
        });

        if (focusYesOnStart)
        {
            yesButton.Select();
        }

        if (noOnEscape)
        {
            InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
                .Subscribe(context =>
                {
                    Close();
                    noAction?.Invoke();
                    
                    // Do not perform further actions, only close the dialog. This action has a higher priority to do so.
                    InputManager.GetInputAction(R.InputActions.usplay_back).CancelNotifyForThisFrame();
                });
        }
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}
