using System;
using UnityEngine;
using UnityEngine.UI;

public class SerializationDemoSceneController : MonoBehaviour
{
    public Text serializedText;
    public Text deserializedText;

    public object loadedInstance;

    DemoSerializable root;
    DemoSerializable child;

    void OnEnable()
    {
        child = new DemoSerializable("child", null);
        root = new DemoSerializable("root", child);

        JsonExample();
        // XmlExample();
    }

    private void XmlExample()
    {
        // Xml serialization of C# has issues with Dictionary and nested objects if not marked explicitly.
        string xml = XmlConverter.ToXml(root);
        serializedText.text = xml;

        loadedInstance = XmlConverter.FromXml<DemoSerializable>(xml);
        deserializedText.text = XmlConverter.ToXml(loadedInstance);
    }

    private void JsonExample()
    {
        string json = JsonConverter.ToJson(root, true);
        serializedText.text = json;

        loadedInstance = JsonConverter.FromJson<DemoSerializable>(json);
        deserializedText.text = JsonConverter.ToJson(loadedInstance, true);
    }
}
