using UnityEngine;
using System;
using LibVLCSharp;

// do note that the VLC screen in this scene has an Unlit/Transparent shader
public class VLCTransparentVideoPlayback : MonoBehaviour
{
    LibVLC _libVLC;
    MediaPlayer _mediaPlayer;
    const int seekTimeDelta = 5000;
    Texture2D tex = null;
    bool playing;
    
    void Awake()
    {
        Core.Initialize(Application.dataPath);

        _libVLC = new LibVLC(enableDebugLogs: true);

        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        //_libVLC.Log += (s, e) => UnityEngine.Debug.Log(e.FormattedLog); // enable this for logs in the editor

        PlayPause();
    }

    public void SeekForward()
    {
        Debug.Log("[VLC] Seeking forward !");
        _mediaPlayer.SetTime(_mediaPlayer.Time + seekTimeDelta);
    }

    public void SeekBackward()
    {
        Debug.Log("[VLC] Seeking backward !");
        _mediaPlayer.SetTime(_mediaPlayer.Time - seekTimeDelta);
    }

    void OnDisable() 
    {
        _mediaPlayer?.Stop();
        _mediaPlayer?.Dispose();
        _mediaPlayer = null;

        _libVLC?.Dispose();
        _libVLC = null;
    }

    public void PlayPause()
    {
        Debug.Log ("[VLC] Toggling Play Pause !");
        if (_mediaPlayer == null)
        {
            _mediaPlayer = new MediaPlayer(_libVLC);
        }
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
        }
        else
        {
            playing = true;

            if(_mediaPlayer.Media == null)
            {
                var media = new Media(new Uri("https://github.com/mozilla/nestegg/raw/refs/heads/master/test/media/dancer1.webm"));
                _mediaPlayer.Media = media;
            }

            _mediaPlayer.Play();
        }
    }

    public void Stop ()
    {
        Debug.Log ("[VLC] Stopping Player !");

        playing = false;
        _mediaPlayer?.Stop();

        // there is no need to dispose every time you stop, but you should do so when you're done using the mediaplayer and this is how:
        // _mediaPlayer?.Dispose();
        // _mediaPlayer = null;
    }

    void Update()
    {
        if(!playing)
        {
            if (tex != null && _mediaPlayer != null)
                TextureHelper.UpdateTexture(tex, ref _mediaPlayer);
            return;
        }

        if (tex == null)
        {
            // If received size is not null, it and scale the texture
            tex = TextureHelper.CreateNativeTexture(ref _mediaPlayer, linear: true);
            if (tex != null)
            {
                Debug.Log("Creating texture with height " + tex.height + " and width " + tex.width);
                GetComponent<Renderer>().material.mainTexture = tex;
            }
        }
        else
        {
            TextureHelper.UpdateTexture(tex, ref _mediaPlayer);
        }
    }
}
