using System.Collections.Generic;
using UniInject;
using UnityEngine;

public class CommonSceneObjectsBinder : MonoBehaviour, IBinder
{
    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.Bind(typeof(SettingsManager)).ToExistingInstance(SettingsManager.Instance);
        bb.Bind(typeof(Settings)).ToExistingInstance(SettingsManager.Instance.Settings);
        return bb.GetBindings();
    }
}
