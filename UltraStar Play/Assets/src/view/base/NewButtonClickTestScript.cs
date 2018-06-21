using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

[Serializable]
public class MySaveData
{
    private int lalalala;
    float health;
}

public class NewButtonClickTestScript : MonoBehaviour
{

    //private MySaveData saveData;

    public void SaveTheGame()
    {
        using (var file = new FileStream("filename", FileMode.Create))
        {
            //XmlSerializer ser = new XmlSerializer(typeof(MySaveData));
            //ser.Serialize(file, saveData);
        }
    }

    public override bool Equals(object other)
    {
        return base.Equals(other);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString();
    }

    // Use this for initialization
    void Start ()
    {
        // todo
    }

    // Update is called once per frame
    void Update ()
    {
        // todo
    }

    public void NewButtonClickTest()
    {
        this.enabled = !this.enabled;
    }
}
