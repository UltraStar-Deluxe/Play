using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UniInject
{
    public class Injector
    {
        public Injector ParentInjector { get; private set; }

        private readonly List<IBinding> bindings = new List<IBinding>();

        public Injector(Injector parent)
        {
            this.ParentInjector = parent;
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
            }
            catch (Exception e)
            {
                throw new InjectionException($"Cannot inject {target}.{memberInfo.Name}: " + e.Message, e);
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
                throw new InjectionException($"Cannot inject {target}.{memberInfo.Name}. Only Fields, Properties and Methods are supported for injection.");
            }
        }

        internal object[] GetValuesForConstructorInjection(Type type)
        {
            return null;
        }

        private object[] GetValuesToBeInjected(object[] bindingKeys)
        {
            object[] valuesToBeInjected = new object[bindingKeys.Length];
            int index = 0;
            foreach (object bindingKey in bindingKeys)
            {
                IBinding binding = GetBinding(bindingKey);
                IProvider provider = binding.GetProvider();

                // Providers that create new instances must inject newly created instances with the injector.
                if (provider is NewInstancesProvider)
                {
                    (provider as NewInstancesProvider).SetInjector(this);
                }

                object valueToBeInjected = provider.Get();
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
