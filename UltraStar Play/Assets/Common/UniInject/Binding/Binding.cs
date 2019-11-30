namespace UniInject
{
    public class Binding : IBinding
    {
        private readonly object key;
        private readonly IProvider provider;

        public Binding(object key, IProvider provider)
        {
            this.key = key;
            this.provider = provider;
        }

        public object GetKey()
        {
            return key;
        }

        public IProvider GetProvider()
        {
            return provider;
        }
    }
}