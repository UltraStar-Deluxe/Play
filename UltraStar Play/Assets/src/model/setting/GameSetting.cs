using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSetting
{
    private readonly Dictionary<ESetting, System.Object> m_settings = new Dictionary<ESetting, System.Object>();

    public GameSetting()
    {
        m_settings = InitSettingDefaults(m_settings);
    }

    private static Dictionary<ESetting, System.Object> InitSettingDefaults(Dictionary<ESetting, System.Object> settings)
    {
        settings.Clear();

        settings.Add(ESetting.ActualLineColor, Color.white);
        settings.Add(ESetting.ActualLineOColor, Color.yellow);
        settings.Add(ESetting.AskbeforeDel, true);
        settings.Add(ESetting.AudioOutputBufferSize, (short)-1);
        settings.Add(ESetting.BackgroundMusic, true);
        settings.Add(ESetting.BeatClick, false);
        settings.Add(ESetting.ClickAssist, false);
        settings.Add(ESetting.ThemeColor, "blue");
        settings.Add(ESetting.Debug, true);
        settings.Add(ESetting.EffectSing, true);
        settings.Add(ESetting.FullScreen, true);
        settings.Add(ESetting.JukeboxShowLyrics, true);
        settings.Add(ESetting.Language, ELanguage.English);
        settings.Add(ESetting.LineBonus, true);
        settings.Add(ESetting.LoadAnimation, true);
        settings.Add(ESetting.LyricsAlpha, (float)0.9d);
        settings.Add(ESetting.LyricsEffect, ELyricsEffect.Slide);
        settings.Add(ESetting.LyricsFont, "default");
        settings.Add(ESetting.MicBoost, (char)12); // decibel
        settings.Add(ESetting.MovieSize, EMovieSize.FullBackgroundAndVid);
        settings.Add(ESetting.NextLineColor, Color.grey);
        settings.Add(ESetting.NextLineOColor, Color.white);
        settings.Add(ESetting.OnSongClick, EOnSongClick.Sing);
        settings.Add(ESetting.Oscilloscope, true);
        settings.Add(ESetting.Players, (char)6);
        settings.Add(ESetting.PreviewFading, (char)3); // seconds
        settings.Add(ESetting.PreviewVolume, (float)0.7d); // percentage of max app volume
        settings.Add(ESetting.Resolution, "800x600");
        settings.Add(ESetting.SavePlayback, false);
        settings.Add(ESetting.ShowScores, true);
        settings.Add(ESetting.SingLineColor, Color.yellow);
        settings.Add(ESetting.SingLineOColor, Color.white);
        settings.Add(ESetting.SingScores, true);
        settings.Add(ESetting.SingTimebarMode, ESingTimebarMode.Remaining);
        settings.Add(ESetting.Skin, "Summer");
        settings.Add(ESetting.SongDir, @"C:\Program Files (x86)\MyLittleKaraoke_test2\songs");
        settings.Add(ESetting.SongMenu, ESongMenu.Roulette);
        settings.Add(ESetting.Sorting, ESorting.Artist);
        settings.Add(ESetting.Tabs, false);
        settings.Add(ESetting.Theme, "Modern");
        settings.Add(ESetting.Threshold, (float)0.15); // Thresshold as percentage of maximum loudness.
        settings.Add(ESetting.TopScores, ETopScores.All);
        settings.Add(ESetting.VideoEnabled, true); // enables / disables video playback support in SSingView & SJukeboxView
        settings.Add(ESetting.VideoPreview, true);
        settings.Add(ESetting.Visualization, EVisualisation.Off);
        settings.Add(ESetting.VoicePassthrough, false);
        settings.Add(ESetting.WebcamBrightness, (short)0);
        settings.Add(ESetting.WebcamEffect, (short)0);
        settings.Add(ESetting.WebcamFlip, true);
        settings.Add(ESetting.WebcamFps, (short)30);
        settings.Add(ESetting.WebcamHue, (short)0);
        settings.Add(ESetting.WebcamId, (short)0);
        settings.Add(ESetting.WebcamResolution, "800x600");
        settings.Add(ESetting.WebcamSaturation, (short)0);

        foreach (ESetting key in settings.Keys)
        {
            System.Object value;
            settings.TryGetValue(key, out value);
            if (value == null)
            {
                throw new UnityException("Default value for a ESetting in GameSettings source code is missing!");
            }
        }

        return settings;
    }

    public System.Object GetSettingNotNull(ESetting key)
    {
        System.Object value;
        m_settings.TryGetValue(key, out value);
        if(value == null)
        {
            throw new UnityException("Illegal ESetting as setting!");
        }
        return value;
    }

    public void SetSetting(ESetting key, System.Object value)
    {
        m_settings[key] = value;
    }
}
