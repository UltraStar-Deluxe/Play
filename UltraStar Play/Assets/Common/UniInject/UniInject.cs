using System;
using System.Collections.Generic;

namespace UniInject
{
    public class UniInject
    {
        // Global injector
        public static Injector GlobalInjector { get; set; } = new Injector(null);

        // Injector for the current scene. The SceneInjectionManager will create and remove the instance.
        public static Injector SceneInjector { get; internal set; }

        // Holds information how to instantiate objects of types during Dependency Injection.
        // This includes the parameters that must be resolved to call the constructor.
        private static readonly Dictionary<Type, ConstructorInjectionData> constructorInjectionDatas = new Dictionary<Type, ConstructorInjectionData>();

        public static ConstructorInjectionData GetConstructorInjectionData(Type type)
        {
            bool foundInjectionData = constructorInjectionDatas.TryGetValue(type, out ConstructorInjectionData injectionData);
            if (!foundInjectionData)
            {
                injectionData = ReflectionUtils.CreateConstructorInjectionData(type);
                constructorInjectionDatas[type] = injectionData;
            }
            return injectionData;
        }

        // Creates a new Injector.
        // If no parent is given, then the GlobalInjector is used as parent.
        public static Injector CreateInjector(Injector parent = null)
        {
            if (parent != null)
            {
                return new Injector(parent);
            }
            else
            {
                return new Injector(GlobalInjector);
            }
        }
    }
}