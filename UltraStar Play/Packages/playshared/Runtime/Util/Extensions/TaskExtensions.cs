using System.Threading.Tasks;
using UnityEngine;

public static class TaskExtensions
{
    public static async Awaitable AsAwaitable(this Task a)
    {
        await a;
    }

    public static async Awaitable<T> AsAwaitable<T>(this Task<T> a)
    {
        return await a;
    }
}
