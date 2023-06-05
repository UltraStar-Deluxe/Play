public static class ArrayUtils
{
    public static void SafeSet<T>(T[] array, T value, int index)
    {
        if (array == null 
            || index < 0
            || index >= array.Length)
        {
            return;
        }
        
        array[index] = value;
    }
    
    public static T SafeGet<T>(T[] array, int index, T fallbackValue)
    {
        if (array == null 
            || index < 0
            || index >= array.Length)
        {
            return fallbackValue;
        }
        
        return array[index];
    }
}
