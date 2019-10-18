using UnityEngine;

public class ReadonlyVsNotReadonlyField : MonoBehaviour
{
    // The value of x can be viewed in the Inspector in debug mode, although it is private.
    private int x = 1;
    // However, the value of y cannot be viewed in the Inspector in debug mode, because it is readonly.
    private readonly int y = 2;

    void Start()
    {
        Debug.Log("x: " + x);
        Debug.Log("y: " + y);
    }
}
