namespace UniInject
{
    public class InstanceProvider<T> : IProvider
    {
        private readonly T instance;

        public InstanceProvider(T instance)
        {
            this.instance = instance;
        }

        public object Get()
        {
            return instance;
        }
    }
}