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

    public void SetColorOfMicProfile(MicProfile micProfile)
    {
        GetComponentInChildren<Image>().color = micProfile.Color;
    }
}
