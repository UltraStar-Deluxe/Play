using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniInject.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniInject
{
    public class SceneBindingManager : MonoBehaviour
    {
        private readonly List<IBinder> binders = new List<IBinder>();
        private readonly List<InjectionData> injectionDatas = new List<InjectionData>();

        void Awake()
        {
            // (1) Iterate over scene hierarchy, thereby
            // (a) find InjectorConfig instances
            // (b) find objects that need injection
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                AnalyzeScriptsRecursively(rootObject);
            }

            // (2) Store bindings of InjectorConfigs in the GlobalInjector
            foreach (IBinder injectorConfig in binders)
            {
                List<IBinding> bindings = injectorConfig.GetBindings();
                foreach (IBinding binding in bindings)
                {
                    UniInject.GlobalInjector.AddBinding(binding);
                }
            }

            // (3) Inject the bindings from the GlobalInjector into the objects that need injection.
            foreach (InjectionData memberToBeInjected in injectionDatas)
            {
                UniInject.GlobalInjector.InjectMember(memberToBeInjected.TargetObject, memberToBeInjected.MemberInfo, memberToBeInjected.InjectionKeys);
            }
        }

        private void AnalyzeScriptsRecursively(GameObject gameObject)
        {
            MonoBehaviour[] scripts = gameObject.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts.Where(it => it != null))
            {
                if (script is IBinder)
                {
                    binders.Add(script as IBinder);
                }
                AnalyzeInjectionAnnotations(script);
            }

            foreach (Transform child in gameObject.transform)
            {
                AnalyzeScriptsRecursively(child.gameObject);
            }
        }

        private void AnalyzeInjectionAnnotations(MonoBehaviour script)
        {
            Type type = script.GetType();
            MemberInfo[] memberInfos = type.GetMembers(BindingFlags.Public
                                                  | BindingFlags.NonPublic
                                                  | BindingFlags.Instance);
            foreach (MemberInfo memberInfo in memberInfos)
            {
                AnalyzeInjectAttribute(script, memberInfo);
                AnalyzeInjectComponentAttribute(script, memberInfo);
            }
        }

        private void AnalyzeInjectAttribute(MonoBehaviour script, MemberInfo memberInfo)
        {
            InjectAttribute injectAttribute = memberInfo.GetCustomAttribute<InjectAttribute>();
            if (injectAttribute != null)
            {
                InjectionData injectionData = null;
                if (memberInfo is FieldInfo || memberInfo is PropertyInfo)
                {
                    injectionData = CreateInjectionDataForFieldOrProperty(script, memberInfo, injectAttribute);
                }
                else if (memberInfo is MethodInfo)
                {
                    injectionData = CreateInjectionDataForMethod(script, memberInfo as MethodInfo, injectAttribute);
                }

                if (injectionData != null)
                {
                    injectionDatas.Add(injectionData);
                }
            }
        }

        private InjectionData CreateInjectionDataForMethod(MonoBehaviour script, MethodInfo methodInfo, InjectAttribute injectAttribute)
        {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            object[] injectionKeys = new object[parameterInfos.Length];
            foreach (ParameterInfo parameterInfo in parameterInfos)
            {
                InjectionKeyAttribute injectionKeyAttribute = parameterInfo.GetCustomAttribute<InjectionKeyAttribute>();
                object injectionKey;
                if (injectionKeyAttribute != null)
                {
                    injectionKey = injectionKeyAttribute.key;
                }
                else
                {
                    Type typeOfParameter = parameterInfo.ParameterType;
                    injectionKey = typeOfParameter;
                }
                int parameterIndex = parameterInfo.Position;
                injectionKeys[parameterIndex] = injectionKey;
            }
            InjectionData injectionData = new InjectionData(script, methodInfo, injectionKeys);
            return injectionData;
        }

        private InjectionData CreateInjectionDataForFieldOrProperty(MonoBehaviour script, MemberInfo memberInfo, InjectAttribute injectAttribute)
        {
            object injectionKey = injectAttribute.key;
            if (injectionKey == null)
            {
                Type typeOfMember = GetTypeOfFieldOrProperty(script, memberInfo);
                injectionKey = typeOfMember;
            }
            object[] injectionKeys = new object[] { injectionKey };
            InjectionData injectionData = new InjectionData(script, memberInfo, injectionKeys);
            return injectionData;
        }

        private Type GetTypeOfFieldOrProperty(MonoBehaviour script, MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
            {
                return (memberInfo as FieldInfo).FieldType;
            }
            else if (memberInfo is PropertyInfo)
            {
                return (memberInfo as PropertyInfo).PropertyType;
            }
            throw new InjectionException($"Member is not supported for injection: {script.name}.{memberInfo.Name}");
        }

        private void AnalyzeInjectComponentAttribute(MonoBehaviour script, MemberInfo memberInfo)
        {
            InjectComponentAttribute injectComponentAttribute = memberInfo.GetCustomAttribute<InjectComponentAttribute>();
            if (injectComponentAttribute != null)
            {
                DoComponentInjection(script, memberInfo, injectComponentAttribute);
            }
        }

        private void DoComponentInjection(MonoBehaviour script, MemberInfo memberInfo, InjectComponentAttribute injectComponentAttribute)
        {
            Type componentType = GetTypeOfFieldOrProperty(script, memberInfo);
            object component = null;
            switch (injectComponentAttribute.GetComponentMethod)
            {
                case GetComponentMethods.GetComponent:
                    component = script.GetComponent(componentType);
                    break;
                case GetComponentMethods.GetComponentInChildren:
                    component = script.GetComponentInChildren(componentType);
                    break;
                case GetComponentMethods.GetComponentInParent:
                    component = script.GetComponentInParent(componentType);
                    break;
                case GetComponentMethods.FindObjectOfType:
                    component = FindObjectOfType(componentType);
                    break;
            }
            if (component != null)
            {
                if (memberInfo is FieldInfo)
                {
                    (memberInfo as FieldInfo).SetValue(script, component);
                }
                else if (memberInfo is PropertyInfo)
                {
                    (memberInfo as PropertyInfo).SetValue(script, component);
                }
                else
                {
                    throw new Exception($"Cannot inject member {script.name}.{memberInfo}."
                                       + " Only Fields and Properties are supported for component injection via Unity methods.");
                }
            }
            else
            {
                Debug.LogError($"Cannot inject member {script.name}.{memberInfo.Name}."
                              + $" No component of type {componentType} found using method {injectComponentAttribute.GetComponentMethod}");
            }
        }

        private class InjectionData
        {
            // The object that needs injection. The member belongs to this object.
            public object TargetObject { get; private set; }

            // The member of the target object that needs injection.
            public MemberInfo MemberInfo { get; private set; }

            // A method can have a multiple parameters and all of them have to be injected.
            // Thus, there can be multiple injectionKeys for a member.
            public object[] InjectionKeys { get; private set; }

            public InjectionData(object targetObject, MemberInfo memberInfo, object[] injectionKeys)
            {
                this.TargetObject = targetObject;
                this.MemberInfo = memberInfo;
                this.InjectionKeys = injectionKeys;
            }
        }
    }
}