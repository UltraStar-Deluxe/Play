// Marker interface to declare that a class should not be injected by the SceneInjectionManager,
// even though it has the INeedInjection marker interface.
// Can be used to opt-out from SceneInjection, e.g., when manual injection is done later.
public interface IExcludeFromSceneInjection
{
}
