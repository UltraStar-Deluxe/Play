using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UniRx;

public class WarningDialog : Dialog
{
    public Button okButton;

    public bool focusOkButtonOnStart = true;

    private List<IDisposable> disposables = new List<IDisposable>();
    
    void Start()
    {
        okButton.OnClickAsObservable().Subscribe(_ => Close());
        if (focusOkButtonOnStart)
        {
            okButton.Select();
        }
        
        disposables.Add(InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(6)
            .Subscribe(context =>
            {
                Close();
                // Do not perform further actions, only close the dialog. This action has a higher priority to do so.
                InputManager.GetInputAction(R.InputActions.usplay_back).CancelNotifyForThisFrame();
            }));
    }

    public void Close()
    {
        disposables.ForEach(it => it.Dispose());
        Destroy(gameObject);
    }
}
