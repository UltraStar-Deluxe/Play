using UniInject;
using UnityEngine;

public static class UniInjectExtensions
{
    public static void InjectAllComponentsInChildren(this Injector injector, MonoBehaviour monoBehaviour)
    {
        foreach (INeedInjection childThatNeedsInjection in monoBehaviour.GetComponentsInChildren<INeedInjection>())
        {
            injector.Inject(childThatNeedsInjection);
        }
    }
}
