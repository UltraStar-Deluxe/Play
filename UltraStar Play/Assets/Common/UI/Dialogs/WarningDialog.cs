using PrimeInputActions;
using UniRx;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WarningDialog : Dialog
{
    public Button okButton;

    public bool focusOkButtonOnStart = true;

    void Start()
    {
        okButton.OnClickAsObservable().Subscribe(_ => Close());
        if (focusOkButtonOnStart)
        {
            okButton.Select();
        }
        
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(6)
            .Subscribe(OnBack)
            .AddTo(gameObject);
    }

    public void Close()
    {
        Destroy(gameObject);
    }

    private void OnBack(InputAction.CallbackContext context)
    {
        Close();
        // Do not perform further actions, only close the dialog. This action has a higher priority to do so.
        InputManager.GetInputAction(R.InputActions.usplay_back).CancelNotifyForThisFrame();
    }
}
