using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoScript : MonoBehaviour {

    public GameObject objOriginal;
    public GameObject objGlow;
    public GameObject objOutline;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ToggleOriginal(bool selected)
    {
        objOriginal.SetActive(selected);
        objGlow.SetActive(!selected);
        objOutline.SetActive(!selected);
    }

    public void ToggleGlow(bool selected)
    {
        objOriginal.SetActive(!selected);
        objGlow.SetActive(selected);
        objOutline.SetActive(!selected);
    }

    public void ToggleOutline(bool selected)
    {
        objOriginal.SetActive(!selected);
        objGlow.SetActive(!selected);
        objOutline.SetActive(selected);
    }

}
