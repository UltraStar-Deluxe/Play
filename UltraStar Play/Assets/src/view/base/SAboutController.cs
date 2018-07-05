using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SAboutController : MonoBehaviour
{
    /// <summary>
    /// Use this for initialization.
    /// </summary>
	void Start ()
    {
        Debug.Log("beeeeeep");
    }
	
	public void GoToMainView()
    {
        SceneManager.LoadScene("src/view/base/SMainView");
    }
}
