using UnityEngine;
using UnityEngine.UI;

public class TotalScoreDisplayer : MonoBehaviour
{
    private Text text;

    void Awake()
    {
        text = GetComponentInChildren<Text>();
    }

    public void ShowTotalScore(int score)
    {
        text.text = score.ToString();
    }

    public void SetColor(Color color)
    {
        GetComponentInChildren<ImageHueHelper>().SetHueByColor(color);
    }
}
