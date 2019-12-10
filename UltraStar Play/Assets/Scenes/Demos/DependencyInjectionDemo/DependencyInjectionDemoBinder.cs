using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;

public class DependencyInjectionDemoBinder : MonoBehaviour, IBinder
{
    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.Bind("author").ToExistingInstance("Tolkien");
        bb.Bind(typeof(int)).ToExistingInstance(42);
        bb.Bind("personWithAge").ToExistingInstance("Bob");
        bb.Bind(typeof(IDependencyInjectionDemoInterface)).ToNewInstancesOfType(typeof(DependencyInjectionDemoInterfaceImpl));
        bb.Bind(typeof(IDependencyInjectionDemoInterfaceWithConstructorParameters)).ToSingleInstanceOfType(typeof(DependencyInjectionDemoInterfaceWithConstructorParametersImpl));
        return bb.GetBindings();
    }
}
