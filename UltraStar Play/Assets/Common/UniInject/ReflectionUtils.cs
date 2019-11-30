using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UniInject
{
    public static class ReflectionUtils
    {
        public static List<InjectionData> CreateInjectionDatas(object obj)
        {
            List<InjectionData> result = new List<InjectionData>();

            Type type = obj.GetType();
            MemberInfo[] memberInfos = type.GetMembers(BindingFlags.Public
                                                  | BindingFlags.NonPublic
                                                  | BindingFlags.Instance);
            foreach (MemberInfo memberInfo in memberInfos)
            {
                InjectionData newInjectionData = CreateInjectionData(obj, memberInfo);
                if (newInjectionData != null)
                {
                    result.Add(newInjectionData);
                }
            }
            return result;
        }

        public static InjectionData CreateInjectionData(object obj, MemberInfo memberInfo)
        {
            InjectAttribute injectAttribute = memberInfo.GetCustomAttribute<InjectAttribute>();
            InjectionData result = null;
            if (injectAttribute != null)
            {
                if (memberInfo is FieldInfo || memberInfo is PropertyInfo)
                {
                    result = ReflectionUtils.CreateInjectionDataForFieldOrProperty(obj, memberInfo, injectAttribute);

                }
                else if (memberInfo is MethodInfo)
                {
                    result = ReflectionUtils.CreateInjectionDataForMethod(obj, memberInfo as MethodInfo, injectAttribute);
                }
            }
            return result;
        }

        public static InjectionData CreateInjectionDataForFieldOrProperty(object obj, MemberInfo memberInfo, InjectAttribute injectAttribute)
        {
            object injectionKey = injectAttribute.key;
            if (injectionKey == null)
            {
                Type typeOfMember = GetTypeOfFieldOrProperty(obj, memberInfo);
                injectionKey = typeOfMember;
            }
            object[] injectionKeys = new object[] { injectionKey };
            InjectionData injectionData = new InjectionData(obj, memberInfo, injectionKeys);
            return injectionData;
        }

        public static InjectionData CreateInjectionDataForMethod(object obj, MethodInfo methodInfo, InjectAttribute injectAttribute)
        {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            object[] injectionKeys = new object[parameterInfos.Length];
            foreach (ParameterInfo parameterInfo in parameterInfos)
            {
                object injectionKey = GetInjectionKey(parameterInfo);
                int parameterIndex = parameterInfo.Position;
                injectionKeys[parameterIndex] = injectionKey;
            }
            InjectionData injectionData = new InjectionData(obj, methodInfo, injectionKeys);
            return injectionData;
        }

        public static ConstructorInjectionData CreateConstructorInjectionData(Type type)
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

        public static ConstructorInjectionData CreateConstructorInjectionData(Type type, ConstructorInfo constructorInfo)
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

        public static object GetInjectionKey(ParameterInfo parameterInfo)
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

        public static Type GetTypeOfFieldOrProperty(object obj, MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
            {
                return (memberInfo as FieldInfo).FieldType;
            }
            else if (memberInfo is PropertyInfo)
            {
                return (memberInfo as PropertyInfo).PropertyType;
            }
            throw new InjectionException($"Member is not supported for injection: {obj.GetType()}.{memberInfo.Name}");
        }
    }
}