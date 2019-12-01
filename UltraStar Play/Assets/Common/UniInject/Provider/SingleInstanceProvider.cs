using System;

namespace UniInject
{
    public class SingleInstanceProvider : IProvider
    {
        private object singleInstance;
        private readonly NewInstancesProvider newInstancesProvider;

        public SingleInstanceProvider(Type type)
        {
            newInstancesProvider = new NewInstancesProvider(type);
        }

        public object Get(Injector injector, out bool resultNeedsInjection)
        {
            if (singleInstance == null)
            {
                singleInstance = newInstancesProvider.Get(injector, out resultNeedsInjection);
            }
            else
            {
                resultNeedsInjection = false;
            }
            return singleInstance;
        }
    }
}