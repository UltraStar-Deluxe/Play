using NUnit.Framework;

public static class AssertUtils
{
    public static void HasType<T>(object obj)
    {
        Assert.IsNotNull(obj);
        Assert.IsTrue(obj is T, $"expected type {typeof(T)} but was {obj.GetType()} on instance '{obj}'");
    }
}
