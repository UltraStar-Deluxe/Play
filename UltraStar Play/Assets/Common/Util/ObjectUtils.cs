public static class ObjectUtils
{
    // Swaps the values of a and b.
    // Example: Let a=1 and b=2. Then Swap(ref a, ref b) will result in a=2 and b=1.
    public static void Swap<T>(ref T a, ref T b)
    {
        T tmp = a;
        a = b;
        b = tmp;
    }
}