using UniInject;
using UnityEngine;

public static class UniInjectExtensions
{
    public static void InjectAllComponentsInChildren(this Injector injector, GameObject gameObject)
    {
        foreach (INeedInjection childThatNeedsInjection in gameObject.GetComponentsInChildren<INeedInjection>())
        {
            injector.Inject(childThatNeedsInjection);
        }
    }

    public static void InjectAllComponentsInChildren(this Injector injector, MonoBehaviour monoBehaviour)
    {
        InjectAllComponentsInChildren(injector, monoBehaviour.gameObject);
    }
}
