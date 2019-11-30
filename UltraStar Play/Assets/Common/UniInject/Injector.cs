using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UniInject
{
    public class Injector
    {
        public Injector ParentInjector { get; private set; }

        private readonly List<IBinding> bindings = new List<IBinding>();

        private HashSet<Type> getValuesForConstructorInjectionVisitedTypes = new HashSet<Type>();

        public Injector(Injector parent)
        {
            this.ParentInjector = parent;
        }

        public T GetInstance<T>()
        {
            object result = GetInstance(typeof(T));
            if (result is T t)
            {
                return t;
            }
            throw new InjectionException($"Cannot create instance of type {typeof(T)}. No binding found.");
        }

        public object GetInstance(object bindingKey)
        {
            IBinding binding = GetBinding(bindingKey);
            IProvider provider = binding.GetProvider();

            // A provider that creates new instances must inject newly created instances with the injector.
            if (provider is NewInstancesProvider)
            {
                (provider as NewInstancesProvider).SetInjector(this);
            }

            object result = provider.Get();
            return result;
        }

        public void InjectAll(object target)
        {
            // Find all members to be injected via reflection.
            List<InjectionData> injectionDatas = ReflectionUtils.CreateInjectionDatas(target);

            // Inject existing bindings into the fields.
            foreach (InjectionData injectionData in injectionDatas)
            {
                Inject(injectionData);
            }
        }

        public void Inject(InjectionData injectionData)
        {
            if (injectionData.searchMethod == SearchMethods.SearchInBindings)
            {
                InjectMemberFromBindings(injectionData.TargetObject, injectionData.MemberInfo, injectionData.InjectionKeys, injectionData.isOptional);
            }
            else if (injectionData.TargetObject is MonoBehaviour)
            {
                InjectMemberFromUnitySearchMethod(injectionData.TargetObject as MonoBehaviour, injectionData.MemberInfo, injectionData.searchMethod, injectionData.isOptional);
            }
            else
            {
                throw new InjectionException($"Cannot perform injection via {injectionData.searchMethod} into an object of type {injectionData.TargetObject.GetType()}."
                    + " Only MonoBehaviour instances are supported.");
            }
        }

        private void InjectMemberFromBindings(object target, MemberInfo memberInfo, object[] bindingKeys, bool isOptional)
        {
            object[] valuesToBeInjected;
            try
            {
                if (isOptional)
                {
                    try
                    {
                        valuesToBeInjected = GetValuesToBeInjected(bindingKeys);
                    }
                    catch (MissingBindingException)
                    {
                        // Ignore because the injection is optional.
                        return;
                    }
                }
                else
                {
                    valuesToBeInjected = GetValuesToBeInjected(bindingKeys);
                }

                if (valuesToBeInjected == null)
                {
                    throw new InjectionException("No values to be injected.");
                }

                if (memberInfo is FieldInfo)
                {
                    (memberInfo as FieldInfo).SetValue(target, valuesToBeInjected[0]);
                }
                else if (memberInfo is PropertyInfo)
                {
                    (memberInfo as PropertyInfo).SetValue(target, valuesToBeInjected[0]);
                }
                else if (memberInfo is MethodInfo)
                {
                    (memberInfo as MethodInfo).Invoke(target, valuesToBeInjected);
                }
                else
                {
                    throw new InjectionException($"Only Fields, Properties and Methods are supported for injection.");
                }

            }
            catch (Exception e)
            {
                throw new InjectionException($"Cannot inject {target}.{memberInfo.Name}: " + e.Message, e);
            }
        }

        private void InjectMemberFromUnitySearchMethod(MonoBehaviour script, MemberInfo memberInfo, SearchMethods strategy, bool isOptional)
        {
            Type componentType = ReflectionUtils.GetTypeOfFieldOrProperty(script, memberInfo);
            object component = null;
            switch (strategy)
            {
                case SearchMethods.GetComponent:
                    component = script.GetComponent(componentType);
                    break;
                case SearchMethods.GetComponentInChildren:
                    component = script.GetComponentInChildren(componentType);
                    break;
                case SearchMethods.GetComponentInParent:
                    component = script.GetComponentInParent(componentType);
                    break;
                case SearchMethods.FindObjectOfType:
                    component = GameObject.FindObjectOfType(componentType);
                    break;
                default:
                    throw new InjectionException($"Cannot inject {script.name}.{memberInfo.Name}."
                        + $" Unkown Unity search method {strategy}");
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
                        + $" Only Fields and Properties are supported for component injection via Unity methods.");
                }
            }
            else if (!isOptional)
            {
                throw new Exception($"Cannot inject member {script.name}.{memberInfo.Name}."
                    + $" No component of type {componentType} found using method {strategy}");
            }
        }

        internal object[] GetValuesForConstructorInjection(Type type)
        {
            if (getValuesForConstructorInjectionVisitedTypes.Contains(type))
            {
                throw new InjectionException($"Circular dependencies in the constructor parameters of type {type}");
            }
            getValuesForConstructorInjectionVisitedTypes.Add(type);

            ConstructorInjectionData constructorInjectionData = UniInject.GetConstructorInjectionData(type);
            object[] bindingKeys = constructorInjectionData.InjectionKeys;
            object[] result = GetValuesToBeInjected(bindingKeys);

            getValuesForConstructorInjectionVisitedTypes.Remove(type);
            return result;
        }

        private object[] GetValuesToBeInjected(object[] bindingKeys)
        {
            if (bindingKeys == null)
            {
                return null;
            }

            object[] valuesToBeInjected = new object[bindingKeys.Length];
            int index = 0;
            foreach (object bindingKey in bindingKeys)
            {
                object valueToBeInjected = GetInstance(bindingKey);
                valuesToBeInjected[index] = valueToBeInjected ?? throw new InjectionException($"Value to be injected for key {bindingKey} is null");
                index++;
            }
            return valuesToBeInjected;
        }

        private IBinding GetBinding(object bindingKey)
        {
            List<IBinding> matchingBindings = bindings.Where(it => it.GetKey().Equals(bindingKey)).ToList();
            if (matchingBindings.Count == 0)
            {
                if (ParentInjector != null)
                {
                    return ParentInjector.GetBinding(bindingKey);
                }
                throw new MissingBindingException("Missing binding for key " + bindingKey);
            }
            else if (matchingBindings.Count > 1)
            {
                throw new MultipleBindingsException("Multiple bindings for key " + bindingKey);
            }

            return matchingBindings[0];
        }

        public void AddBinding(IBinding binding)
        {
            bindings.Add(binding);
        }
    }
}
