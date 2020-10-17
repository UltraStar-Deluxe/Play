using UnityEngine;
using UnityEngine.UI;

public class ShowFps : MonoBehaviour
{
    public Text fpsText;

    [ReadOnly]
    public int fps;

    private float deltaTime;
    private int frameCount;

    void Update()
    {
        frameCount++;
        deltaTime += Time.deltaTime;

        if (deltaTime >= 0.5f)
        {
            fps = (int)Mathf.Ceil(frameCount / deltaTime);
            frameCount = 0;
            deltaTime -= 0.5f;

            if (fpsText != null)
            {
                fpsText.text = "FPS: " + fps.ToString();
            }
        }
    }
}
