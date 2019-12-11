using System;
using System.Reflection;

namespace UniInject
{
    public class ConstructorInjectionData
    {
        // The type that is instantiated by the constructor.
        public Type type { get; private set; }

        // A constructor can have multiple parameters and all of them have to be injected.
        // Thus, there can be multiple injectionKeys for a constructor.
        public object[] InjectionKeys { get; private set; }

        public ConstructorInjectionData(Type type, object[] injectionKeys)
        {
            this.type = type;
            this.InjectionKeys = injectionKeys;
        }
    }
}