namespace UniInject
{
    /**
     * Unambiguous name for the UniInject.Binding class,
     * which is ambiguous with Unity's own Binding class that exists since the upgrade to Unity 2023.2
     */
    public class UniInjectBinding : UniInject.Binding
    {
        public UniInjectBinding(object key, IProvider provider)
            : base(key, provider)
        {
        }
    }
}
