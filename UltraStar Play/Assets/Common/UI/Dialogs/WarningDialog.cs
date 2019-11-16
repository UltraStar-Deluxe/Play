using UnityEngine.UI;
using UniRx;

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
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}