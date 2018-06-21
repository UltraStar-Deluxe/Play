using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SAboutController : MonoBehaviour
{
	// Use this for initialization
	void Start ()
    {
        System.Console.WriteLine("beeeeeep");
    }
	
	// Update is called once per frame
	void Update ()
    {
        // todo
    }

    public void GoToMainView()
    {
        //Application.c
        SceneManager.LoadScene("src/view/base/SMainView");
    }
}
