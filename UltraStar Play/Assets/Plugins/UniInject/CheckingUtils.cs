using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniInject
{
    // Utility methods for checking consistency with respect to dependency injection.
    // Most methods return the number of found issues.
    public static class CheckingUtils
    {
        public static int CheckCurrentScene()
        {
            int errorCount = 0;

            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            // Find bindings
            List<IBinding> bindings = new List<IBinding>();
            foreach (GameObject rootObject in rootObjects)
            {
                List<IBinding> bindingsInRootObject = FindBindings(rootObject);
                bindings.AddRange(bindingsInRootObject);
            }

            // Check that there is a binding for every value that should be injected.
            // Furthermore, check that all fields and properties with an InjectedInInspectorAttribute
            // actually have a value.
            foreach (GameObject rootObject in rootObjects)
            {
                MonoBehaviour[] scripts = rootObject.GetComponentsInChildren<MonoBehaviour>();
                foreach (MonoBehaviour script in scripts)
                {
                    errorCount += CheckScript(script, bindings);
                }
            }

            return errorCount;
        }

        private static int CheckScript(MonoBehaviour script, List<IBinding> bindings)
        {
            int errorCount = 0;

            Type type = script.GetType();
            MemberInfo[] memberInfos = type.GetMembers(BindingFlags.Public
                                                        | BindingFlags.NonPublic
                                                        | BindingFlags.Instance);

            errorCount += CheckInjectable(script, type, bindings);

            foreach (MemberInfo memberInfo in memberInfos)
            {
                errorCount += CheckInjectedInInspectorAttribute(script, type, memberInfo);
            }

            return errorCount;
        }

        private static int CheckInjectable(MonoBehaviour script, Type type, List<IBinding> bindings)
        {
            int errorCount = 0;

            List<InjectionData> injectionDatas = UniInjectUtils.GetInjectionDatas(type);
            foreach (InjectionData injectionData in injectionDatas)
            {
                if (injectionData.isOptional)
                {
                    continue;
                }

                if (injectionData.searchMethod == SearchMethods.SearchInBindings)
                {
                    errorCount += CheckInjectableFromBindings(script, type, bindings, injectionData);
                }
                else
                {
                    errorCount += CheckInjectableFromUnitySearchMethod(script, type, injectionData);
                }
            }

            return errorCount;
        }

        private static int CheckInjectableFromUnitySearchMethod(MonoBehaviour script, Type type, InjectionData injectionData)
        {
            if (injectionData.InjectionKeys.Length > 1)
            {
                // If there are multiple keys, then it must be for a method or constructor with multiple parameters
                LogErrorCannotBeInjected($"The search method {injectionData.searchMethod} can only be used on a field or property.",
                    script, type, injectionData.MemberInfo);
                return 1;
            }

            object injectionKey = injectionData.InjectionKeys[0];
            if (!(injectionKey is Type))
            {
                LogErrorCannotBeInjected($"The search method {injectionData.searchMethod} can not be used with a custom key.",
                    script, type, injectionData.MemberInfo);
                return 1;
            }
            Type componentType = injectionKey as Type;

            UnityEngine.Object searchResult = UniInjectUtils.InvokeUnitySearchMethod(script, injectionData.searchMethod, componentType);
            if (searchResult == null)
            {
                LogErrorCannotBeInjected($"No instance of {componentType} found using {injectionData.searchMethod}.",
                    script, type, injectionData.MemberInfo);
                return 1;
            }

            return 0;
        }

        private static int CheckInjectableFromBindings(MonoBehaviour script, Type type, List<IBinding> bindings, InjectionData injectionData)
        {
            foreach (object key in injectionData.InjectionKeys)
            {
                List<IBinding> matchingBindings = bindings.Where(binding => object.Equals(binding.GetKey(), key)).ToList();
                if (matchingBindings.Count == 0)
                {
                    LogErrorCannotBeInjected($"Missing binding for key {key}",
                        script, type, injectionData.MemberInfo);
                    return 1;
                }
                else if (matchingBindings.Count > 1)
                {
                    LogErrorCannotBeInjected($"Multiple bindings for key {key}",
                        script, type, injectionData.MemberInfo);
                    return 1;
                }
            }
            return 0;
        }

        private static int CheckInjectedInInspectorAttribute(MonoBehaviour script, Type type, MemberInfo memberInfo)
        {
            InjectedInInspectorAttribute attribute = memberInfo.GetCustomAttribute<InjectedInInspectorAttribute>();
            if (attribute == null)
            {
                return 0;
            }

            // Check that the value has been set.
            object value = null;
            if (memberInfo is FieldInfo)
            {
                value = (memberInfo as FieldInfo).GetValue(script);
            }
            else if (memberInfo is PropertyInfo)
            {
                value = (memberInfo as PropertyInfo).GetValue(script);
            }
            else
            {
                // This should never happen
                // because the attribute can only be used on fields and properties.
                return 0;
            }

            if (value == null || value.ToString() == "null")
            {
                Debug.LogError($"<b>{type.Name}.{memberInfo.Name}</b> of {script.name} is null."
                    + "\nIf this is intended then remove the InjectedInInspector attribute.", script);
                return 1;
            }
            return 0;
        }

        private static void LogErrorCannotBeInjected(string reason, MonoBehaviour script, Type type, MemberInfo memberInfo)
        {
            Debug.LogError($"<b>{type.Name}.{memberInfo.Name}</b> of {script.name} cannot be injected. {reason}", script);
        }

        private static List<IBinding> FindBindings(GameObject rootObject)
        {
            List<IBinding> result = new List<IBinding>();
            IBinder[] binders = rootObject.GetComponentsInChildren<IBinder>();
            foreach (IBinder binder in binders)
            {
                List<IBinding> newBindings = binder.GetBindings();
                result.AddRange(newBindings);
            }
            return result;
        }

    }
}