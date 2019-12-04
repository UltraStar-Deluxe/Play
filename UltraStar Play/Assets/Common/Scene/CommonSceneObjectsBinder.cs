using System.Collections.Generic;
using UniInject;
using UnityEngine;

public class CommonSceneObjectsBinder : MonoBehaviour, IBinder
{
    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(SettingsManager.Instance);
        return bb.GetBindings();
    }
}
