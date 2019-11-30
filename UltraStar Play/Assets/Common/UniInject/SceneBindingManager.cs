using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniInject;
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
            // (a) find IBinder instances.
            // (b) find objects that need injection and how their members should be injected.
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                AnalyzeScriptsRecursively(rootObject);
            }

            // (2) Store bindings in the GlobalInjector
            foreach (IBinder binder in binders)
            {
                List<IBinding> bindings = binder.GetBindings();
                foreach (IBinding binding in bindings)
                {
                    UniInject.GlobalInjector.AddBinding(binding);
                }
            }

            // (4) Inject the bindings from the GlobalInjector into the objects that need injection.
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
                List<InjectionData> newInjectionDatas = ReflectionUtils.CreateInjectionDatas(script);
                injectionDatas.AddRange(newInjectionDatas);
            }

            foreach (Transform child in gameObject.transform)
            {
                AnalyzeScriptsRecursively(child.gameObject);
            }
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
            Type componentType = ReflectionUtils.GetTypeOfFieldOrProperty(script, memberInfo);
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
    }
}