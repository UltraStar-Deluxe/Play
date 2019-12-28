using UnityEngine;
using UnityEngine.UI;
using UniRx;

[RequireComponent(typeof(Text))]
public class SliderValueLabel : MonoBehaviour
{
    public Slider slider;

    private Text text;

    void Awake()
    {
        text = GetComponent<Text>();
    }

    void Start()
    {
        slider.OnValueChangedAsObservable().Subscribe(newValue => text.text = newValue.ToString("F2"));
    }

}
