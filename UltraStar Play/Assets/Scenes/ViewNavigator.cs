using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ViewNavigator : MonoBehaviour
{
    public void QuitGame()
    {
        // TODO ask for confirmation if required
        Application.Quit();
    }

    public void SwitchToMainView()
    {
        SceneManager.LoadScene((int)EScreen.SMainView);
    }

    public void SwitchToLoadingView()
    {
        SceneManager.LoadScene((int)EScreen.SLoadingView);
    }

    public void SwitchToAboutView()
    {
        SceneManager.LoadScene((int)EScreen.SAboutView);
    }

    public void SwitchToSetupPlayerView()
    {
        SceneManager.LoadScene((int)EScreen.SSetupPlayerView);
    }

    public void SwitchToHighscoreView()
    {
        SceneManager.LoadScene((int)EScreen.SHighscoreView);
    }

    public void SwitchToResultSongView()
    {
        SceneManager.LoadScene((int)EScreen.SResultSongView);
    }

    public void SwitchToStatsView()
    {
        SceneManager.LoadScene((int)EScreen.SStatsView);
    }

    public void SwitchToOptionsView()
    {
        SceneManager.LoadScene((int)EScreen.SOptionsView);
    }

    public void SwitchToOptionsGameView()
    {
        SceneManager.LoadScene((int)EScreen.SOptionsGameView);
    }

    public void SwitchToOptionsGraphicsView()
    {
        SceneManager.LoadScene((int)EScreen.SOptionsGraphicsView);
    }

    public void SwitchToOptionsSoundView()
    {
        SceneManager.LoadScene((int)EScreen.SOptionsSoundView);
    }

    public void SwitchToSingView()
    {
        SceneManager.LoadScene((int)EScreen.SSingView);
    }

    public void SwitchToSongFolderView()
    {
        SceneManager.LoadScene((int)EScreen.SSongFolderView);
    }

    public void SwitchToSongPlaylistView()
    {
        SceneManager.LoadScene((int)EScreen.SSongPlaylistView);
    }

    public void SwitchToSongSelectView()
    {
        SceneManager.LoadScene((int)EScreen.SSongSelectView);
    }

    public void SwitchToJukeboxView()
    {
        SceneManager.LoadScene((int)EScreen.SJukeboxView);
    }
}
