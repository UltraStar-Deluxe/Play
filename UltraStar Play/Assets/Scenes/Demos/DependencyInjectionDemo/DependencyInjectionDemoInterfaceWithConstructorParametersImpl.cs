using System.Collections.Generic;
using UniInject;

public class DependencyInjectionDemoInterfaceWithConstructorParametersImpl : IDependencyInjectionDemoInterfaceWithConstructorParameters
{
    private readonly string name;

    // This constructor should not be used for instantiation during dependency injection, because it is not annotated.
    public DependencyInjectionDemoInterfaceWithConstructorParametersImpl(List<string> names)
    {
        this.name = names[0];
    }

    [Inject]
    public DependencyInjectionDemoInterfaceWithConstructorParametersImpl([InjectionKey("author")] string name)
    {
        this.name = name;
    }

    public string GetByeBye()
    {
        return "Bye bye " + name + "!";
    }
}