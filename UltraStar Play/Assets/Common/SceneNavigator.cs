using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator 
{
    public static void LoadScene(EScreen screen)
    {
        SceneManager.LoadScene((int)screen);
    }
}
