using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UniRx;
using UnityEngine.InputSystem;

public class WarningDialog : Dialog
{
    public Button okButton;

    public bool focusOkButtonOnStart = true;

    private readonly List<IDisposable> disposables = new List<IDisposable>();
    
    void Start()
    {
        okButton.OnClickAsObservable().Subscribe(_ => Close());
        if (focusOkButtonOnStart)
        {
            okButton.Select();
        }
        
        disposables.Add(InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(6)
            .Subscribe(OnBack));
    }

    public void Close()
    {
        disposables.ForEach(it => it.Dispose());
        Destroy(gameObject);
    }

    private void OnBack(InputAction.CallbackContext context)
    {
        Close();
        // Do not perform further actions, only close the dialog. This action has a higher priority to do so.
        InputManager.GetInputAction(R.InputActions.usplay_back).CancelNotifyForThisFrame();
    }
}
