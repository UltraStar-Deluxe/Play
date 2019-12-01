using System.Collections.Generic;
using UniInject;
using UnityEngine;

public class CommonSceneObjectsBinder : MonoBehaviour, IBinder
{
    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        SettingsManager settingsManager = SettingsManager.Instance;
        bb.Bind(typeof(SettingsManager)).ToExistingInstance(settingsManager);
        return bb.GetBindings();
    }
}
