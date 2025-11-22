using UnityEngine;
using System;
using System.Threading.Tasks;
using LibVLCSharp;

public class VLCThreeSixty : MonoBehaviour
{
    LibVLC _libVLC;
    MediaPlayer _mediaPlayerScreen;
    MediaPlayer _mediaPlayerSphere;
    const int seekTimeDelta = 5000;
    Texture2D texScreen, texSphere;
    Renderer screen, sphere;
    bool playing;
    float Yaw, Pitch, Roll;

    void Awake()
    {
        Core.Initialize(Application.dataPath);
        _libVLC = new LibVLC(enableDebugLogs: true);

        screen = GameObject.Find("Screen").GetComponent<Renderer>();
        sphere = GameObject.Find("Sphere").GetComponent<Renderer>();
    }

    void Start() => PlayPause();

    void OnDisable()
    {
        _mediaPlayerScreen?.Stop();
        _mediaPlayerScreen?.Dispose();
        _mediaPlayerSphere?.Dispose();
        _libVLC?.Dispose();
    }

    public void PlayPause()
    {
        if (_mediaPlayerScreen == null)
        {
            // keep default projection for the screen, thus with viewpoint navigation enabled
            _mediaPlayerScreen = new MediaPlayer(_libVLC);
        }
        if (_mediaPlayerSphere == null)
        {
            // disable default projection for the sphere, no libvlc viewpoint navigation
            _mediaPlayerSphere = new MediaPlayer(_libVLC);
            _mediaPlayerSphere.SetProjectionMode(VideoProjection.Rectangular);
        }

        Debug.Log("[VLC] Toggling Play Pause!");

        if (_mediaPlayerScreen.IsPlaying || _mediaPlayerSphere.IsPlaying)
        {
            _mediaPlayerScreen.Pause();
            _mediaPlayerSphere.Pause();
        }
        else
        {
            playing = true;
            if (_mediaPlayerScreen.Media == null)
            {
                // better save the media locally to avoid slow networks
                var media = new Media(new Uri("https://streams.videolan.org/streams/360/eagle_360.mp4"));
                Task.Run(async () =>
                {
                    var result = await media.ParseAsync(_libVLC, MediaParseOptions.ParseNetwork);
                    Debug.Log(media.TrackList(TrackType.Video)[0].Data.Video.Projection == VideoProjection.Equirectangular
                        ? "The video is a 360 video" : "The video was not identified as a 360 video by VLC");
                });
                _mediaPlayerScreen.Media = _mediaPlayerSphere.Media = media;
            }
            _mediaPlayerScreen.Play();
            _mediaPlayerSphere.Play();
        }
    }

    void Update()
    {
        if (!playing) return;
        UpdateTexture(ref texScreen, _mediaPlayerScreen, screen);
        UpdateTexture(ref texSphere, _mediaPlayerSphere, sphere);
    }

    void UpdateTexture(ref Texture2D texture, MediaPlayer player, Renderer renderer)
    {
        if (texture == null)
        {
            uint width = 0, height = 0;
            player.Size(0, ref width, ref height);
            var texPtr = player.GetTexture(width, height, out bool updated);

            if (width != 0 && height != 0 && updated && texPtr != IntPtr.Zero)
            {
                Debug.Log($"Creating texture with height {height} and width {width}");
                texture = Texture2D.CreateExternalTexture((int)width, (int)height, TextureFormat.RGBA32, false, true, texPtr);
                renderer.material.mainTexture = texture;
            }
        }
        else
        {
            var texPtr = player.GetTexture((uint)texture.width, (uint)texture.height, out bool updated);
            if (updated) texture.UpdateExternalTexture(texPtr);
        }
    }

    void OnGUI() => Do360Navigation();

    void Do360Navigation()
    {
        var range = Math.Max(UnityEngine.Screen.width, UnityEngine.Screen.height);
        Yaw = _mediaPlayerScreen.Viewpoint.Yaw;
        Pitch = _mediaPlayerScreen.Viewpoint.Pitch;
        Roll = _mediaPlayerScreen.Viewpoint.Roll;

        if (Input.GetKey(KeyCode.RightArrow)) RotateView(20f, 80 * 40 / range, 0);
        else if (Input.GetKey(KeyCode.LeftArrow)) RotateView(-20f, -80 * 40 / range, 0);
        else if (Input.GetKey(KeyCode.DownArrow)) RotateView(0, 0, 1);
        else if (Input.GetKey(KeyCode.UpArrow)) RotateView(0, 0, -1);
    }

    void RotateView(float sphereRotation, float yawDelta, float pitchDelta)
    {
        // no viewpoint changes for the sphere as the whole video is shown on the texture
        // we can rotate the texture instead
        sphere.transform.Rotate(Vector3.up * sphereRotation * Time.deltaTime);
        sphere.transform.Rotate(Vector3.right * pitchDelta * 10 * Time.deltaTime);

        Pitch = Mathf.Clamp(Pitch + pitchDelta, -90f, 90f);    
        _mediaPlayerScreen.UpdateViewpoint(Yaw + yawDelta, Pitch, Roll, 80);
    }

}