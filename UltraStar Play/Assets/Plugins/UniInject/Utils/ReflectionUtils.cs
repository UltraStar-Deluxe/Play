using System;
using System.Collections.Generic;
using System.Reflection;

namespace UniInject
{
    internal static class ReflectionUtils
    {
        internal static List<InjectionData> CreateInjectionDatas(Type type)
        {
            List<InjectionData> result = new List<InjectionData>();

            MemberInfo[] memberInfos = type.GetMembers(BindingFlags.Public
                                                  | BindingFlags.NonPublic
                                                  | BindingFlags.Instance);
            foreach (MemberInfo memberInfo in memberInfos)
            {
                InjectionData newInjectionData = CreateInjectionData(type, memberInfo);
                if (newInjectionData != null)
                {
                    result.Add(newInjectionData);
                }
            }
            return result;
        }

        private static InjectionData CreateInjectionData(Type type, MemberInfo memberInfo)
        {
            InjectAttribute injectAttribute = memberInfo.GetCustomAttribute<InjectAttribute>();
            InjectionData result = null;
            if (injectAttribute != null)
            {
                if (memberInfo is FieldInfo || memberInfo is PropertyInfo)
                {
                    result = ReflectionUtils.CreateInjectionDataForFieldOrProperty(type, memberInfo, injectAttribute);

                }
                else if (memberInfo is MethodInfo)
                {
                    result = ReflectionUtils.CreateInjectionDataForMethod(type, memberInfo as MethodInfo, injectAttribute);
                }
            }
            return result;
        }

        private static InjectionData CreateInjectionDataForFieldOrProperty(Type type, MemberInfo memberInfo, InjectAttribute injectAttribute)
        {
            object injectionKey = injectAttribute.key;
            if (injectionKey == null)
            {
                Type typeOfMember = GetTypeOfFieldOrProperty(type, memberInfo);
                injectionKey = typeOfMember;
            }
            object[] injectionKeys = new object[] { injectionKey };
            InjectionData injectionData = new InjectionData(type, memberInfo, injectionKeys, injectAttribute.searchMethod, injectAttribute.optional);
            return injectionData;
        }

        private static InjectionData CreateInjectionDataForMethod(Type type, MethodInfo methodInfo, InjectAttribute injectAttribute)
        {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            object[] injectionKeys = new object[parameterInfos.Length];
            foreach (ParameterInfo parameterInfo in parameterInfos)
            {
                object injectionKey = GetInjectionKey(parameterInfo);
                int parameterIndex = parameterInfo.Position;
                injectionKeys[parameterIndex] = injectionKey;
            }
            InjectionData injectionData = new InjectionData(type, methodInfo, injectionKeys, injectAttribute.searchMethod, injectAttribute.optional);
            return injectionData;
        }

        internal static ConstructorInjectionData CreateConstructorInjectionData(Type type)
        {
            ConstructorInfo[] constructorInfos = type.GetConstructors(BindingFlags.Public
                                                                    | BindingFlags.Instance);
            if (constructorInfos.Length == 0)
            {
                // Use the default constructor
                return new ConstructorInjectionData(type, null);
            }
            else if (constructorInfos.Length == 1)
            {
                // If only one constructor, then use it.
                return CreateConstructorInjectionData(type, constructorInfos[0]);
            }
            else
            {
                // If multiple constructors, then use the one that is marked with an inject annotation.
                ConstructorInfo constructorInfoWithInjectAttribute = null;
                foreach (ConstructorInfo constructorInfo in constructorInfos)
                {
                    InjectAttribute injectAttribute = constructorInfo.GetCustomAttribute<InjectAttribute>();
                    if (injectAttribute != null)
                    {
                        if (constructorInfoWithInjectAttribute == null)
                        {
                            constructorInfoWithInjectAttribute = constructorInfo;
                        }
                        else
                        {
                            // There should only be one constructor with an inject annotation.
                            // Otherwise, it is not clear how to instantiate objects of the class.
                            throw new InjectionException($"There should only be one public constructor annotated with Inject, but found multiple for type {type}");
                        }
                    }
                }

                if (constructorInfoWithInjectAttribute != null)
                {
                    return CreateConstructorInjectionData(type, constructorInfoWithInjectAttribute);
                }
                else
                {
                    throw new InjectionException($"Multiple public constructors found for type {type}."
                    + " It is unclear how to instantiate objects of the type."
                    + " Add an Inject annotation to the constructor that should be used.");
                }
            }
        }

        private static ConstructorInjectionData CreateConstructorInjectionData(Type type, ConstructorInfo constructorInfo)
        {
            ParameterInfo[] parameterInfos = constructorInfo.GetParameters();
            object[] injectionKeys = new object[parameterInfos.Length];
            foreach (ParameterInfo parameterInfo in parameterInfos)
            {
                object injectionKey = GetInjectionKey(parameterInfo);
                int parameterIndex = parameterInfo.Position;
                injectionKeys[parameterIndex] = injectionKey;
            }
            ConstructorInjectionData result = new ConstructorInjectionData(type, injectionKeys);
            return result;
        }

        private static object GetInjectionKey(ParameterInfo parameterInfo)
        {
            InjectionKeyAttribute injectionKeyAttribute = parameterInfo.GetCustomAttribute<InjectionKeyAttribute>();
            if (injectionKeyAttribute != null)
            {
                return injectionKeyAttribute.key;
            }
            else
            {
                return parameterInfo.ParameterType;
            }
        }

        internal static Type GetTypeOfFieldOrProperty(object obj, MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
            {
                return (memberInfo as FieldInfo).FieldType;
            }
            else if (memberInfo is PropertyInfo)
            {
                return (memberInfo as PropertyInfo).PropertyType;
            }
            throw new ArgumentException($"Member is neither a field nor a property: {obj.GetType()}.{memberInfo.Name}");
        }
    }
}