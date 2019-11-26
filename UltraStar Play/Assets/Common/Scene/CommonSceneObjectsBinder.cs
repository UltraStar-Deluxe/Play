using System.Collections.Generic;
using UniInject;
using UnityEngine;

public class CommonSceneObjectsBinder : MonoBehaviour, IBinder
{
    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.Bind(typeof(SettingsManager)).ToInstance(SettingsManager.Instance);
        bb.Bind(typeof(Settings)).ToInstance(SettingsManager.Instance.Settings);
        return bb.GetBindings();
    }
}
