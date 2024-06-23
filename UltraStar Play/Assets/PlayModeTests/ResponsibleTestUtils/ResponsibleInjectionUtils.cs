using NUnit.Framework;
using Responsible;
using UniInject;
using static Responsible.Responsibly;

public class ResponsibleInjectionUtils
{
    public static ITestInstruction<T> GetValueForInjectionKey<T>(Injector injector = null) where T : class
        => DoAndReturn(
            $"get value for type '{typeof(T)}' from injector '{GetInjectorOrSceneInjection(injector)}",
            () =>
            {
                T result = GetInjectorOrSceneInjection(injector).GetValueForInjectionKey<T>();
                Assert.IsNotNull(result);
                return result;
            });

    public static ITestInstruction<object> GetValueForInjectionKey(object injectionKey, Injector injector = null)
        => DoAndReturn(
            $"get value for injection key '{injectionKey}' from injector '{GetInjectorOrSceneInjection(injector)}'",
            () =>
            {
                object result = GetInjectorOrSceneInjection(injector).GetValueForInjectionKey(injectionKey);
                Assert.IsNotNull(result);
                return result;
            });

    private static Injector GetInjectorOrSceneInjection(Injector injector)
    {
        return injector != null
            ? injector
            : UltraStarPlaySceneInjectionManager.Instance.SceneInjector;
    }
}
