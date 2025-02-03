using System;
using System.Threading.Tasks;
using UniRx;

public static class ObservableExtensions
{
    public static async Task<T> FirstAsync<T>(this IObservable<T> source)
    {
        Task<T> task = source
            .Take(1)
            .ToTask();

        // If no value is emitted, ToTask() will throw an exception
        return await task;
    }

    public static async Task<T> FirstOrDefaultAsync<T>(this IObservable<T> source)
    {
        var task = source
            .Take(1)
            .ToTask();

        try
        {
            // Thrown if the observable is empty
            return await task;
        }
        catch (InvalidOperationException)
        {
            // Return default value if empty
            return default(T);
        }
    }
}
