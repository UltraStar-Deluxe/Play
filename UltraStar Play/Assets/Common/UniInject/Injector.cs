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

        public void InjectMembers(object target)
        {
            // Find all members to be injected via reflection.

            // Inject existing bindings into the fields.
        }

        public void InjectField(object target, FieldInfo fieldInfo, object bindingKey)
        {
            InjectMember(target, fieldInfo, new object[] { bindingKey });
        }

        public void InjectProperty(object target, PropertyInfo propertyInfo, object bindingKey)
        {
            InjectMember(target, propertyInfo, new object[] { bindingKey });
        }

        public void InjectMethod(object target, MethodInfo propertyInfo, object[] bindingKeys)
        {
            InjectMember(target, propertyInfo, bindingKeys);
        }

        public void InjectMember(object target, MemberInfo memberInfo, object[] bindingKeys)
        {
            object[] valuesToBeInjected;
            try
            {
                valuesToBeInjected = GetValuesToBeInjected(bindingKeys);
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
