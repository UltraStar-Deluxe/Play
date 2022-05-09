using System.Collections.Generic;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

public class CommonSceneObjectsBinder : MonoBehaviour, IBinder
{
    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(ApplicationManager.Instance);
        bb.BindExistingInstance(SettingsManager.Instance);
        bb.BindExistingInstance(CoroutineManager.Instance);
        bb.BindExistingInstance(ClientSideConnectRequestManager.Instance);
        bb.BindExistingInstance(InputManager.Instance);
        bb.BindExistingInstance(TranslationManager.Instance);
        bb.BindExistingInstance(GetUiDocument());

        bb.BindExistingInstance(SettingsManager.Instance.Settings);
        bb.Bind(    typeof(ISettings)).ToExistingInstance(SettingsManager.Instance.Settings);

        return bb.GetBindings();
    }

    private static UIDocument GetUiDocument()
    {
        GameObject uiDocGameObject = GameObject.FindWithTag("UIDocument");
        if (uiDocGameObject != null)
        {
            return uiDocGameObject.GetComponent<UIDocument>();
        }
        return null;
    }
}
