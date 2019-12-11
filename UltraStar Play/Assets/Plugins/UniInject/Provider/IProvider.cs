namespace UniInject
{
    public interface IProvider
    {
        object Get(Injector injector, out bool resultNeedsInjection);
    }
}