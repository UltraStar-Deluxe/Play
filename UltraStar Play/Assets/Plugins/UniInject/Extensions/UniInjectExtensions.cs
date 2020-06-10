using UniInject;
using UnityEngine;

public static class UniInjectExtensions
{
    public static void InjectAllComponentsInChildren(this Injector injector, GameObject gameObject, bool includeInactive = false)
    {
        foreach (INeedInjection childThatNeedsInjection in gameObject.GetComponentsInChildren<INeedInjection>(includeInactive))
        {
            injector.Inject(childThatNeedsInjection);
        }
    }

    public static void InjectAllComponentsInChildren(this Injector injector, MonoBehaviour monoBehaviour, bool includeInactive = false)
    {
        InjectAllComponentsInChildren(injector, monoBehaviour.gameObject, includeInactive);
    }
}
