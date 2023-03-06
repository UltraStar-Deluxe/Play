using UnityEngine;

// Marker Interface
public class CommonSceneObjects : MonoBehaviour
{
    public static CommonSceneObjects instance;
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
