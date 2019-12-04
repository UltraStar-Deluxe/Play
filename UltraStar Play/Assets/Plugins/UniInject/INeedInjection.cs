namespace UniInject
{
    // Marker interface to indicate that a class needs injection of some fields, properties, or methods.
    // Using such an interface can improve performance when analyzing a scene for scripts that need injection.
    //
    // As alternative, all types of an assembly can be analyzed once at startup (see AssemblyInjectionDataLoader).
    // The lookup whether a type needs injection will be very fast afterwards.
    public interface INeedInjection
    {

    }
}