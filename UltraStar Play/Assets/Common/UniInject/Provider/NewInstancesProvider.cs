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

            object result;
            object[] constructorParameters = injector.GetValuesForConstructorInjection(type);
            if (constructorParameters == null)
            {
                result = Activator.CreateInstance(type);
            }
            else
            {
                // Instantiate with constructor injection
                result = Activator.CreateInstance(type, constructorParameters);
            }
            resultNeedsInjection = true;
            return result;
        }
    }
}