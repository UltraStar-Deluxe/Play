using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JsonDemoSceneController : MonoBehaviour
{
    public Text uiText;

    public JsonDemoSerializable loadedInstance;

    void Start()
    {
        JsonDemoSerializable root = new JsonDemoSerializable();
        JsonDemoSerializable child = new JsonDemoSerializable();
        root.otherSerializable = child;

        string json = JsonUtility.ToJson(root, true);
        uiText.text = json;

        loadedInstance = JsonUtility.FromJson<JsonDemoSerializable>(json);
        Debug.Log("loaded: " + JsonUtility.ToJson(loadedInstance));
    }
}
