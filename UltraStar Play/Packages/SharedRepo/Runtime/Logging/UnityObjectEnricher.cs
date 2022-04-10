/**
 * Holds a UnityEngine.Object (e.g. GameObject) to use Serilog logging statement
 * and still have Unity's Debug.Log method be called with a context object.
 * The context object will be highlighted in the inspector, when the log message is clicked in the Unity Console.
 * 
 * Usage:
 * <pre>
 * Log.Logger.ForContext(new UnityObjectEnricher(gameObject)).Information("This is an info with context");
 * 
 * using (LogContext.Push(new UnityObjectEnricher(gameObject)))
 * {
 *     Log.Logger.Information("This is an info with context");
 * }
 * </pre>
 */
public class UnityObjectEnricher : ScalarValueEnricher
{
    public static readonly string unityObjectPropertyName = "unityObject";

    public UnityObjectEnricher(UnityEngine.Object value)
        : base(unityObjectPropertyName, value)
    {
    }
}
