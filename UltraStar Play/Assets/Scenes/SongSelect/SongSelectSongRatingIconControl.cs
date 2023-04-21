using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine.UIElements;

public class SongSelectSongRatingIconControl : INeedInjection
{
    [Inject(UxmlName = R.UxmlNames.songRatingStarIcon)]
    private List<VisualElement> songRatingStarIcons;
    
    [Inject]
    private Statistics statistics;
    
    public void HideSongRatingIcons()
    {
        songRatingStarIcons.ForEach(it => it.HideByDisplay());
    }
    
    public void UpdateSongRatingIcons(SongMeta selectedSong, EDifficulty difficulty)
    {
        LocalStatistic localStatistic = statistics.GetLocalStats(selectedSong);
        if (localStatistic == null)
        {
            HideSongRatingIcons();
            return;
        }
        
        List<SongStatistic> topScores = localStatistic.StatsEntries.GetTopScores(1, difficulty);
        List<int> topScoreNumbers = topScores.Select(it => it.Score).ToList();
        if (topScoreNumbers.IsNullOrEmpty())
        {
            HideSongRatingIcons();
            return;
        }
        
        int topScore = topScoreNumbers.FirstOrDefault();
        int starCount = GetStarCount(topScore);
        for (int i = 0; i < songRatingStarIcons.Count; i++)
        {
            songRatingStarIcons[i].SetVisibleByDisplay(i < starCount);
        }
    }

    public static int GetStarCount(int score)
    {
        if (score > 9000)
        {
            return 5;
        }
        if (score > 8000)
        {
            return 4;
        }
        if (score > 7000)
        {
            return 3;
        }
        if (score > 5000)
        {
            return 2;
        }
        if (score > 3000)
        {
            return 1;
        }
        return 0;
    }
}
