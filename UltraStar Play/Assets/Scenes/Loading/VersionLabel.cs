using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
[ExecuteInEditMode]
public class VersionLabel : MonoBehaviour
{
    public string prefix;
    public string suffix;

    void OnEnable()
    {
        Text text = GetComponent<Text>();
        text.text = prefix + ApplicationUtils.AppVersion + suffix;
    }
}
