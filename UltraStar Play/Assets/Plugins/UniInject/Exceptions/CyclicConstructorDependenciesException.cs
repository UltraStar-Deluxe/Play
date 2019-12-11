namespace UniInject
{
    public class CyclicConstructorDependenciesException : InjectionException
    {
        public CyclicConstructorDependenciesException(string message) : base(message)
        {
        }
    }
}