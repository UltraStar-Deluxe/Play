using UnityEngine;

public class DependencyInjectionDemoInterfaceImpl : IDependencyInjectionDemoInterface
{
    private static int instanceCount;

    private readonly int instanceIndex;

    public DependencyInjectionDemoInterfaceImpl()
    {
        instanceIndex = instanceCount;
        instanceCount++;
    }

    public string GetGreeting()
    {
        return $"Hello world from instance {instanceIndex}!";
    }

    public override string ToString()
    {
        return GetGreeting() + " (DependencyInjectionDemoInterfaceImpl)";
    }
}