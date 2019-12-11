using System;

namespace UniInject
{
    public class ExistingInstanceProvider<T> : IProvider
    {
        private T instance;
        private readonly Func<T> instanceGetter;

        public ExistingInstanceProvider(T instance)
        {
            this.instance = instance;
        }

        // Lazy binding of an instance. The getter is called only when the instance is needed.
        public ExistingInstanceProvider(Func<T> instanceGetter)
        {
            this.instanceGetter = instanceGetter;
        }

        public object Get(Injector injector, out bool resultNeedsInjection)
        {
            if (instance == null && instanceGetter != null)
            {
                instance = instanceGetter.Invoke();
            }

            resultNeedsInjection = false;
            return instance;
        }
    }
}