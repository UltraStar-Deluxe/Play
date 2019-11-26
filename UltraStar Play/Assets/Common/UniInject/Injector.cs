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

        public Injector(Injector parent)
        {
            this.ParentInjector = parent;
        }

        public void InjectMembers(object target)
        {
            // Find all members to be injected via reflection.

            // Inject any existing bindings into the fields.
        }

        public void InjectMember(object target, MemberInfo memberInfo, object bindingKey)
        {
            IBinding matchingBinding;
            try
            {
                matchingBinding = GetBinding(bindingKey);
            }
            catch (Exception e)
            {
                throw new InjectionException($"Cannot inject {target}.{memberInfo.Name}", e);
            }

            object bindingValue = matchingBinding.GetProvider().Get();
            if (bindingValue == null)
            {
                throw new InjectionException($"Cannot inject {target}.{memberInfo.Name}. The value for the key {bindingKey} that should be injected is null.");
            }

            if (memberInfo is FieldInfo)
            {
                (memberInfo as FieldInfo).SetValue(target, bindingValue);
            }
            else if (memberInfo is PropertyInfo)
            {
                (memberInfo as PropertyInfo).SetValue(target, bindingValue);
            }
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
