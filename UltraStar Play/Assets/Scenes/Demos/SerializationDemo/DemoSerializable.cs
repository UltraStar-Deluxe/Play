
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DemoSerializable
{
    public string name = "theName";
    public int intNumber = 42;
    public double doubleNumber = 42.5;
    // LitJson does not support serialization of float values (but works in FullSerializer)
    public float floatNumber = 42.5f;
    public string textWithBackslash = @"C:\this\is\a\path.txt";

    // A Dictionary is not serialized to JSON per default in Unity (but works in LitJson).
    // A Dictionary is not serialized using System.Xml.Serialization.
    public Dictionary<string, string> dict = new Dictionary<string, string>();

    public List<string> list = new List<string>();

    // A nested Serializable is not serialized to JSON by Unity (but works in LitJson).
    // A nested Serializable is not serialized using System.Xml.Serialization per default.
    [SerializeField]
    public DemoSerializable otherSerializable;

    public DemoSerializable()
    {
    }

    public DemoSerializable(string name, DemoSerializable child)
    {
        this.name = name;

        dict.Add("foo", "bar");
        dict.Add("bla", "blub");

        list.Add("a");
        list.Add("b");
        list.Add("c");

        otherSerializable = child;
    }
}