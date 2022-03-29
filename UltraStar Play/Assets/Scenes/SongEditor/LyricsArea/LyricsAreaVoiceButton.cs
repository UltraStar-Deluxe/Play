using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LyricsAreaVoiceButton : MonoBehaviour, INeedInjection
{
    public int voiceIndex;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Image image;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;

    [Inject]
    private LyricsAreaControl lyricsAreaControl;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongMeta songMeta;

    void Start()
    {
        if (songMeta.GetVoices().Count <= 1
            || voiceIndex >= songMeta.GetVoices().Count)
        {
            // No need to change voices
            gameObject.SetActive(false);
            return;
        }

        image.color = songEditorSceneControl.GetColorForVoice(GetVoice());
        uiText.text = "P" + (voiceIndex + 1);

        button.OnClickAsObservable().Subscribe(_ => lyricsAreaControl.Voice = GetVoice());
    }

    private Voice GetVoice()
    {
        return songMeta.GetVoices()[voiceIndex];
    }
}
