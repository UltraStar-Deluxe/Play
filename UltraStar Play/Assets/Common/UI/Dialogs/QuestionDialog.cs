using System;
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
    }

    private void Update()
    {
        if (noOnEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            noAction?.Invoke();
            Close();
        }
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}
