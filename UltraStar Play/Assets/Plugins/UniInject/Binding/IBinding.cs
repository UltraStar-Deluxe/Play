namespace UniInject
{
    public interface IBinding
    {
        object GetKey();
        IProvider GetProvider();
    }
}