using System;
using System.Collections.Generic;

namespace UniInject
{
    public class BindingBuilder
    {
        private readonly List<BindingUnderConstruction> bindingsUnderConstruction = new List<BindingUnderConstruction>();

        public BindingBuilder()
        {
        }

        public BindingUnderConstruction Bind(Type key)
        {
            BindingUnderConstruction b = new BindingUnderConstruction(key);
            bindingsUnderConstruction.Add(b);
            return b;
        }

        public BindingUnderConstruction Bind(object key)
        {
            BindingUnderConstruction b = new BindingUnderConstruction(key);
            bindingsUnderConstruction.Add(b);
            return b;
        }

        public List<IBinding> GetBindings()
        {
            List<IBinding> result = new List<IBinding>();
            foreach (BindingUnderConstruction bindingUnderConstruction in bindingsUnderConstruction)
            {
                IBinding binding = bindingUnderConstruction.GetBinding();
                if (binding == null)
                {
                    throw new InjectionException("Unfinished binding for key " + bindingUnderConstruction.GetKey());
                }
                result.Add(binding);
            }

            if (result.Count == 0)
            {
                throw new InjectionException("No bindings in BindingBuilder");
            }
            return result;
        }

        public class BindingUnderConstruction
        {
            private readonly object key;

            private IBinding binding;

            public BindingUnderConstruction(Type key)
            {
                this.key = key;
            }

            public BindingUnderConstruction(object key)
            {
                this.key = key;
            }

            public void ToInstance<T>(T instance)
            {
                IProvider provider = new InstanceProvider<T>(instance);
                IBinding binding = new Binding(key, provider);
                this.binding = binding;
            }

            public void ToProvider(IProvider provider)
            {
                IBinding binding = new Binding(key, provider);
                this.binding = binding;
            }

            public object GetKey()
            {
                return key;
            }

            public IBinding GetBinding()
            {
                return binding;
            }
        }
    }
}