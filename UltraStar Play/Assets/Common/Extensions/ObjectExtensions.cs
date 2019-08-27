
public static class ObjectExtensions {

    public static T OrElse<T>(this T obj, T fallbackObject) {
        if(obj != null) {
            return obj;
        } else {
            return fallbackObject;
        }
    }
}