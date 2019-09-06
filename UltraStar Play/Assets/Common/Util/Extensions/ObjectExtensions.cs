
public static class ObjectExtensions
{

    public static T IfNull<T>(this T obj, T fallbackObject)
    {
        if (obj == null)
        {
            return fallbackObject;
        }
        else
        {
            return obj;
        }
    }
}