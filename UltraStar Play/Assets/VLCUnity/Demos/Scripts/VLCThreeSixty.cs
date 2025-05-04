using UnityEngine;
using System;
using System.Threading.Tasks;
using LibVLCSharp;

public class VLCThreeSixty : MonoBehaviour
{
    LibVLC _libVLC;
    MediaPlayer _mediaPlayer;
    const int seekTimeDelta = 5000;
    Texture2D tex = null;
    bool playing;
    
    float Yaw;
    float Pitch;
    float Roll;

    void Awake()
    {
        Core.Initialize(Application.dataPath);

        _libVLC = new LibVLC(enableDebugLogs: true);

        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        //_libVLC.Log += (s, e) => UnityEngine.Debug.Log(e.FormattedLog); // enable this for logs in the editor

        PlayPause();
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
                // download https://streams.videolan.org/streams/360/eagle_360.mp4 
                // to your computer (to avoid network requests for smoother navigation)
                // and adjust the Uri to the local path
                var media = new Media(new Uri("https://streams.videolan.org/streams/360/eagle_360.mp4"));
                
                Task.Run(async () => 
                {
                    var result = await media.ParseAsync(_libVLC, MediaParseOptions.ParseNetwork);
                    var trackList = media.TrackList(TrackType.Video);
                    var is360 = trackList[0].Data.Video.Projection == VideoProjection.Equirectangular;
                    
                    if(is360)
                        Debug.Log("The video is a 360 video");
                    else
                        Debug.Log("The video was not identified as a 360 video by VLC, make sure it is properly tagged");

                    trackList.Dispose();
                });
                
                _mediaPlayer.Media = media;
            }

            _mediaPlayer.Play();
        }
    }

    void Update()
    {
        if(!playing) return;

        if (tex == null)
        {
            // If received size is not null, it and scale the texture
            uint i_videoHeight = 0;
            uint i_videoWidth = 0;

            _mediaPlayer.Size(0, ref i_videoWidth, ref i_videoHeight);
            var texptr = _mediaPlayer.GetTexture(i_videoWidth, i_videoHeight, out bool updated);
            if (i_videoWidth != 0 && i_videoHeight != 0 && updated && texptr != IntPtr.Zero)
            {
                Debug.Log("Creating texture with height " + i_videoHeight + " and width " + i_videoWidth);
                tex = Texture2D.CreateExternalTexture((int)i_videoWidth,
                    (int)i_videoHeight,
                    TextureFormat.RGBA32,
                    false,
                    true,
                    texptr);
                GetComponent<Renderer>().material.mainTexture = tex;
            }
        }
        else if (tex != null)
        {
            var texptr = _mediaPlayer.GetTexture((uint)tex.width, (uint)tex.height, out bool updated);
            if (updated)
            {
                tex.UpdateExternalTexture(texptr);
            }
        }
    }

    void OnGUI()
    {
        Do360Navigation();
    }
    
    void Do360Navigation()
    {
        var range = Math.Max(UnityEngine.Screen.width, UnityEngine.Screen.height);

        Yaw = _mediaPlayer.Viewpoint.Yaw;
        Pitch = _mediaPlayer.Viewpoint.Pitch;
        Roll = _mediaPlayer.Viewpoint.Roll;
        var fov = 80;

        if (Input.GetKey(KeyCode.RightArrow))
        {
            _mediaPlayer.UpdateViewpoint(Yaw + (float)( 80 * + 40 / range), Pitch, Roll, fov);
        }
        else if(Input.GetKey(KeyCode.LeftArrow))
        {
            _mediaPlayer.UpdateViewpoint(Yaw - (float)( 80 * + 40 / range), Pitch, Roll, fov);
        }
        else if(Input.GetKey(KeyCode.DownArrow))
        {
            _mediaPlayer.UpdateViewpoint(Yaw, Pitch + (float)( 80 * + 20 / range), Roll, fov);
        }
        else if(Input.GetKey(KeyCode.UpArrow))
        {
            _mediaPlayer.UpdateViewpoint(Yaw, Pitch - (float)( 80 * + 20 / range), Roll, fov);
        }
    }
}
