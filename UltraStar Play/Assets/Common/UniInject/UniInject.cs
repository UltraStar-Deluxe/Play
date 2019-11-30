using System;
using System.Collections.Generic;

namespace UniInject
{
    public class UniInject
    {
        public static Injector GlobalInjector { get; set; } = new Injector(null);

        // Holds information how to instantiate objects of types during Dependency Injection.
        // This incldes the parameters that must be resolved to call the constructor.
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
    }
}