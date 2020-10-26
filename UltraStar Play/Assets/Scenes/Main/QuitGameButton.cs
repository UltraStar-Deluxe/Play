using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(Button))]
public class QuitGameButton : MonoBehaviour, INeedInjection
{
    [Inject]
    private UiManager uiManager;

    void Start()
    {
        GetComponent<Button>().OnClickAsObservable()
            .Subscribe(_ =>
            {
                QuestionDialog quitDialog = uiManager.CreateQuestionDialog(
                    I18NManager.GetTranslation(R.String.mainScene_quitDialog_title),
                    I18NManager.GetTranslation(R.String.mainScene_quitDialog_message));
                quitDialog.yesAction = ApplicationUtils.QuitOrStopPlayMode;
            });
    }
}
