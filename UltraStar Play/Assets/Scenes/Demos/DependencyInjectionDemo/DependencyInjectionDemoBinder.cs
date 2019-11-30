using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;

public class DependencyInjectionDemoBinder : MonoBehaviour, IBinder
{
    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.Bind("author").ToInstance("Tolkien");
        bb.Bind(typeof(int)).ToInstance(42);
        bb.Bind("personWithAge").ToInstance("Bob");
        return bb.GetBindings();
    }
}
