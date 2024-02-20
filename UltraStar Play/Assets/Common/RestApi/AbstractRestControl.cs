using SimpleHttpServerForUnity;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class AbstractRestControl : AbstractSingletonBehaviour, INeedInjection
{
    [Inject]
    protected HttpServer httpServer;

    [Inject]
    protected Settings settings;

    [Inject]
    protected NonPersistentSettings nonPersistentSettings;
}
