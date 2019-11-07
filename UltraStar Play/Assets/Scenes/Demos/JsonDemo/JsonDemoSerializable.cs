
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JsonDemoSerializable
{
    public string name = "theName";
    public int intNumber = 42;
    public float floatNumber = 42.5f;

    // A Dictionary is not serialized to JSON per default in Unity.
    public Dictionary<string, string> dict = new Dictionary<string, string>();

    // A non-generic SerializableDictionary with Serializable annotation can be serialized to JSON with Unity.
    public StringToStringMap dict2 = new StringToStringMap();

    public List<string> list = new List<string>();

    // A nested Serializable is not serialized to JSON by Unity.
    [SerializeField]
    public JsonDemoSerializable otherSerializable;

    public JsonDemoSerializable()
    {
        dict.Add("foo", "bar");
        dict.Add("bla", "blub");

        dict2.Add("foo", "bar");
        dict2.Add("bla", "blub");

        list.Add("a");
        list.Add("b");
        list.Add("c");
    }
}