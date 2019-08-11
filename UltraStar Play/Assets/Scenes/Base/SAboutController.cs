using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SAboutController : MonoBehaviour
{
	void Start ()
    {
        Debug.Log("beeeeeep");
    }
	
	public void GoToMainView()
    {
        SceneManager.LoadScene("src/view/base/SMainView");
    }
}
