using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ShowFps : MonoBehaviour
{
    public Text fpsText;
    public float deltaTime;

    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        if (fpsText != null)
        {
            fpsText.text = Mathf.Ceil(fps).ToString();
        }
    }
}