namespace UniInject
{
    public class ExistingInstanceProvider<T> : IProvider
    {
        private readonly T instance;

        public ExistingInstanceProvider(T instance)
        {
            this.instance = instance;
        }

        public object Get(Injector injector, out bool resultNeedsInjection)
        {
            resultNeedsInjection = false;
            return instance;
        }
    }
}