using System;
using System.Collections.Generic;

namespace UniInject
{
    public class UniInjectUtils
    {
        // Global injector
        public static Injector GlobalInjector { get; set; } = new Injector(null);

        // Injector for the current scene. The SceneInjectionManager will create and remove the instance.
        public static Injector SceneInjector { get; internal set; }

        // Holds information how to instantiate objects of types during Dependency Injection.
        // This includes the parameters that must be resolved to call the constructor.
        private static readonly Dictionary<Type, ConstructorInjectionData> typeToConstructorInjectionDataMap = new Dictionary<Type, ConstructorInjectionData>();

        // Holds information about members (fields, properties, methods) that need injection.
        private static readonly Dictionary<Type, List<InjectionData>> typeToInjectionDatasMap = new Dictionary<Type, List<InjectionData>>();

        public static ConstructorInjectionData GetConstructorInjectionData(Type type)
        {
            bool found = typeToConstructorInjectionDataMap.TryGetValue(type, out ConstructorInjectionData injectionData);
            if (!found)
            {
                injectionData = ReflectionUtils.CreateConstructorInjectionData(type);
                typeToConstructorInjectionDataMap.Add(type, injectionData);
            }
            return injectionData;
        }

        public static List<InjectionData> GetInjectionDatas(Type type)
        {
            bool found = typeToInjectionDatasMap.TryGetValue(type, out List<InjectionData> injectionDatas);
            if (!found)
            {
                injectionDatas = ReflectionUtils.CreateInjectionDatas(type);
                typeToInjectionDatasMap.Add(type, injectionDatas);
            }
            return injectionDatas;
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