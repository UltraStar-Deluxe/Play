using System.Threading.Tasks;
using UnityEngine;

public static class HandleUnobservedTaskException
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void StaticInit()
    {
        if (hasRegisteredUnobservedTaskExceptionHandler)
        {
            return;
        }

        hasRegisteredUnobservedTaskExceptionHandler = true;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Debug.Log("Registered UnobservedTaskException callback");
    }

    private static bool hasRegisteredUnobservedTaskExceptionHandler;

    private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Exception(() => e.Exception);
    }

}
