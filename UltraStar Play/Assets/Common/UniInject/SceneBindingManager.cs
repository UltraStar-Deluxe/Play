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
        private readonly List<MemberInjectionData> membersToBeInjected = new List<MemberInjectionData>();

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
            foreach (MemberInjectionData memberToBeInjected in membersToBeInjected)
            {
                UniInject.GlobalInjector.InjectMember(memberToBeInjected.TargetObject, memberToBeInjected.MemberInfo, memberToBeInjected.InjectionKey);
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
                object injectionKey = injectAttribute.key;
                if (injectionKey == null)
                {
                    Type typeOfMember = GetTypeOfMember(memberInfo);
                    injectionKey = typeOfMember;
                }
                MemberInjectionData injectionData = new MemberInjectionData(script, memberInfo, injectionKey);
                membersToBeInjected.Add(injectionData);
            }
        }

        private Type GetTypeOfMember(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
            {
                return (memberInfo as FieldInfo).FieldType;
            }
            else if (memberInfo is PropertyInfo)
            {
                return (memberInfo as PropertyInfo).PropertyType;
            }
            throw new InjectionException("Member is neither field nor property");
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
            Type componentType = GetTypeOfMember(memberInfo);
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
                    throw new Exception("Cannot set value of member " + memberInfo + ". Only Fields and Properties are supported.");
                }
            }
            else
            {
                Debug.LogError($"Could not inject member {script.name}.{memberInfo.Name}."
                              + $" No component of type {componentType} found using method {injectComponentAttribute.GetComponentMethod}");
            }
        }

        private class MemberInjectionData
        {
            public object TargetObject { get; private set; }
            public MemberInfo MemberInfo { get; private set; }
            public object InjectionKey { get; private set; }

            public MemberInjectionData(object targetObject, MemberInfo memberInfo, object injectionKey)
            {
                this.TargetObject = targetObject;
                this.MemberInfo = memberInfo;
                this.InjectionKey = injectionKey;
            }
        }
    }
}