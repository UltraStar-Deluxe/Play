using System;

namespace UniInject
{
    public class NewInstancesProvider : IProvider
    {
        private readonly Type type;

        public NewInstancesProvider(Type type)
        {
            this.type = type;
        }

        public object Get(Injector injector, out bool resultNeedsInjection)
        {
            if (injector == null)
            {
                throw new InjectionException($"Missing Injector for instantiation of new object.");
            }

            object result = injector.Create(type);
            resultNeedsInjection = true;
            return result;
        }
    }
}