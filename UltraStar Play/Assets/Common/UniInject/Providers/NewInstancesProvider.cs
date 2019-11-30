using System;

namespace UniInject
{
    public class NewInstancesProvider : IProvider
    {
        private readonly Type type;

        private Injector injector;

        public NewInstancesProvider(Type type)
        {
            this.type = type;
        }

        public void SetInjector(Injector injector)
        {
            this.injector = injector;
        }

        public virtual object Get()
        {
            object instance = CreateInstance();
            return instance;
        }

        protected object CreateInstance()
        {
            if (injector == null)
            {
                throw new InjectionException("Missing injector for instantiation of new object.");
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

            injector.InjectMembers(result);

            return result;
        }
    }
}