using UnityEngine;
using UnityEngine.UI;

public class HighscoreWebPlayerText : MonoBehaviour
{
    private Text text;

    void Awake()
    {
        text = GetComponent<Text>();
    }

    public void SetText(string value)
    {
        text.text = value;
    }
}
