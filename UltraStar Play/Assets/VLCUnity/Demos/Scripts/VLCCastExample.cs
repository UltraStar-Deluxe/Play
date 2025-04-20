using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections;
using LibVLCSharp;
using UnityEngine.UI;

public class VLCCastExample : MonoBehaviour
{
    public static LibVLC libVLC;
    public Text text;
    private MediaPlayer mediaPlayer;
    private RendererItem rendererItem;
    private RendererDiscoverer rendererDiscoverer;

    public string path = "https://streams.videolan.org//streams/mp4/Mr_MrsSmith-h264_aac.mp4"; //Can be a web path or a local path

    void Awake()
    {
        if (libVLC == null)
            CreateLibVLC();

        CreateMediaPlayer();
    }

    void OnDestroy()
    {
        DestroyMediaPlayer();
    }

    public void Open()
    {
        if ((mediaPlayer != null) && (rendererItem != null))
        {
            ScreenLog("remote media player created");
            // set the previously discovered renderer item (chromecast) on the mediaplayer
            // if you set it to null, it will start to render normally (i.e. locally) again
            if (mediaPlayer.SetRenderer(rendererItem))
            {
                mediaPlayer.Media = new Media(new Uri(path));
            }
        }
    }

    public void Play()
    {
        bool result = mediaPlayer.Play();
        ScreenLog("Play: " + result);
    }

    void CreateLibVLC()
    {
        if (libVLC != null)
        {
            libVLC.Dispose();
            libVLC = null;
        }

        Core.Initialize(Application.dataPath); //Load VLC dlls
        libVLC = new LibVLC(enableDebugLogs: true); //You can customize LibVLC with advanced CLI options here https://wiki.videolan.org/VLC_command-line_help/
        libVLC.Log += (s, e) => ScreenLog(e.FormattedLog);
        libVLC.SetDialogHandlers(
            (dialog, title, text, username, store, token) => Task.CompletedTask,
            (dialog, title, text, type, cancelText, actionText, secondActionText, token) =>
            {
                ScreenLog("QuestionCallback called");
                var result = dialog.PostAction(1);
                ScreenLog("QuestionCallback PostAction " + result.ToString());
                return Task.CompletedTask;
            },
            (dialog, title, text, indeterminate, position, cancelText, token) => Task.CompletedTask,
            (dialog, position, text) => Task.CompletedTask);

        ScreenLog("Changeset: " + libVLC.Changeset);
        ScreenLog("LibVLCSharp version: " + typeof(LibVLC).Assembly.GetName().Version);
    }

    void RendererDiscoverer_ItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
    {
        ScreenLog("ItemAdded");
        rendererItem = e.RendererItem;
        ScreenLog("rendererItem.name = " + rendererItem.Name);
    }

    IEnumerator WaitAndDiscover()
    {
        yield return new WaitForSeconds(5.0f);
        if (mediaPlayer != null)
        {
            ScreenLog("attempt to create RendererDiscoverer ...");
            rendererDiscoverer = new RendererDiscoverer(libVLC);
            if (rendererDiscoverer != null)
            {
                ScreenLog("success.");
                rendererDiscoverer.ItemAdded += RendererDiscoverer_ItemAdded;
                rendererDiscoverer.Start();
            }
        }

        yield return new WaitForSeconds(5.0f);
        if (rendererItem != null)
        {
            Open();
            yield return new WaitForSeconds(5.0f);
            Play();
        }
    }

    void CreateMediaPlayer()
    {
        if (mediaPlayer != null)
        {
            DestroyMediaPlayer();
        }
        mediaPlayer = new MediaPlayer(libVLC);

        StartCoroutine(WaitAndDiscover());
    }

    void DestroyMediaPlayer()
    {
        rendererDiscoverer?.Dispose();
        rendererDiscoverer = null;
        rendererItem = null;
        mediaPlayer?.Stop();
        mediaPlayer?.Dispose();
        mediaPlayer = null;
    }

    void ScreenLog(string message)
    {
        if (text != null)
        {
            text.text += message + "\n";
        }
        Debug.Log(message);
    }
}
