using UnityEngine;

// Marker Interface
public class CommonSceneObjects : MonoBehaviour
{
    private static CommonSceneObjects instance;
    public static CommonSceneObjects Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CommonSceneObjects>();
            }
            return instance;
        }
    }
}
