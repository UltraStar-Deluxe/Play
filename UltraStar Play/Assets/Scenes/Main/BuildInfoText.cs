using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;

[RequireComponent(typeof(Text))]
public class BuildInfoText : MonoBehaviour
{
    void Start()
    {
        GetComponent<Text>().text = "Build: " + Version.buildTimeStamp;
    }
}
