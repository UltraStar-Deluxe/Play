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
    }

    private void JsonExample()
    {
        string json = JsonConverter.ToJson(root, true);
        serializedText.text = json;

        loadedInstance = JsonConverter.FromJson<DemoSerializable>(json);
        deserializedText.text = JsonConverter.ToJson(loadedInstance, true);
    }
}
