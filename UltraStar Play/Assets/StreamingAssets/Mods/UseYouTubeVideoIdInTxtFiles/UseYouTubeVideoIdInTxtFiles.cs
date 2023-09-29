using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UniInject;
using UniRx;
using UnityEngine;

public class UseYouTubeVideoIdTxtFiles : IOnLoadMod, IOnDisableMod
{
    private GameObject gameObject;

    [Inject]
    private Injector injector;

    [Inject]
    private SongMetaManager songMetaManager;

    public void OnLoadMod()
    {
        gameObject = new GameObject();
        gameObject.name = nameof(UseYouTubeVideoIdTxtFilesControl);
        UseYouTubeVideoIdTxtFilesControl control = gameObject.AddComponent<UseYouTubeVideoIdTxtFilesControl>();
        injector.Inject(control);

        // Make it a child of the songMetaManager
        // such that the GameObject is not destroyed when the scene changes.
        gameObject.transform.SetParent(songMetaManager.transform);
    }

    public void OnDisableMod()
    {
        GameObject.Destroy(gameObject);
    }
}

public class UseYouTubeVideoIdTxtFilesControl : MonoBehaviour
{
    // In the txt files on usdb.animux.de, there is
    // - "v=..." if YouTube has a video with audio and video
    // - "a=..." if YouTube has a video with audio only
    private static readonly Regex youTubeVideoIdRegex = new Regex(@"(v|a)=([\w\-_]+)(\r|\n|\,)", RegexOptions.Multiline);

    [Inject]
    private ModObjectContext modObjectContext;

    [Inject]
    private SongMetaManager songMetaManager;

    private HashSet<SongMeta> checkedSongMetas = new HashSet<SongMeta>();
    private int lastSongMetaCount;

    private void Update()
    {
        if (modObjectContext.IsObsolete)
        {
            Destroy(gameObject);
            return;
        }

        IReadOnlyCollection<SongMeta> songMetas = songMetaManager.GetSongMetas();
        if (lastSongMetaCount < songMetas.Count)
        {
            lastSongMetaCount = songMetas.Count;
            List<SongMeta> uncheckedSongMetas = songMetas
                .Except(checkedSongMetas)
                .ToList();
            uncheckedSongMetas.ForEach(songMeta =>
            {
                checkedSongMetas.Add(songMeta);
                UpdateSongWithYouTubeVideoIdFromTxtFiles(songMeta);
            });
        }
    }

    private void UpdateSongWithYouTubeVideoIdFromTxtFiles(SongMeta songMeta)
    {
        if (songMeta == null
            || !songMeta.Website.IsNullOrEmpty()
            || songMeta.FileInfo == null
            || !songMeta.FileInfo.Exists)
        {
            return;
        }

        string txtContent = FileUtils.ReadAllText(songMeta.FileInfo.FullName);
        Match match = youTubeVideoIdRegex.Match(txtContent);
        if (match.Success)
        {
            string videoId = match.Groups[2].Value;
            songMeta.Website = $"https://youtube.com/watch?v={videoId}";
            Debug.Log($"Found YouTube video id for '{SongMetaUtils.GetArtistDashTitle(songMeta)}' in file '{songMeta.FileInfo}'");
        }
    }
}