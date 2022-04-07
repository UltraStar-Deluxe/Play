using UniInject;
using UniInject.Extensions;

public static class UltraStarPlayUniInjectExtensions
{
    public static Injector WithBinding(this Injector injector, IBinding binding)
    {
        Injector childInjector = injector.CreateChildInjector();
        childInjector.AddBinding(binding);
        return childInjector;
    }

    public static Injector WithBindingForInstance<T>(this Injector injector, T existingInstance)
    {
        Injector childInjector = injector.CreateChildInjector();
        childInjector.AddBindingForInstance(existingInstance);
        return childInjector;
    }
}
