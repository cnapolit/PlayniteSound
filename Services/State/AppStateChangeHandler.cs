using Microsoft.Win32;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using System;
using System.Windows;
using PlayniteSounds.Models.State;

namespace PlayniteSounds.Services.State;

public class AppStateChangeHandler(
    IWavePlayerManager wavePlayerManager,
    ISoundPlayer soundPlayer,
    IMusicPlayer musicPlayer,
    PlayniteState playniteState,
    PlayniteSoundsSettings settings)
    : IAppStateChangeHandler
{
    #region Implementation

    #region OnPowerModeChanged

    public void OnPowerModeChanged(object sender, PowerModeChangedEventArgs args)
    {
        if (args.Mode is PowerModes.Resume)
        {
            wavePlayerManager.Init();
        }
    }

    #endregion

    #region OnApplicationDeactivate

    public void OnApplicationDeactivate(object sender, EventArgs e)
    {
        if (settings.PauseOnDeactivate)
        {
            playniteState.HasFocus = false;
            Pause();
        }
    }

    #endregion

    #region OnApplicationActivate

    public void OnApplicationActivate(object sender, EventArgs e)
    {
        if (settings.PauseOnDeactivate && !playniteState.HasFocus)
        {
            playniteState.HasFocus = true;
            Resume();
        }
    }

    #endregion

    #region OnWindowStateChanged

    public void OnWindowStateChanged(object sender, EventArgs e)
    {
        if (settings.PauseOnDeactivate) /* Then */ switch (Application.Current?.MainWindow?.WindowState)
        {
            case WindowState.Normal:
            case WindowState.Maximized:
                if (!playniteState.HasFocus)
                {
                    playniteState.HasFocus = true;
                    Resume();
                }
                break;
            case WindowState.Minimized:
                playniteState.HasFocus = false;
                Pause();
                break;
        }
    }

    #endregion

    #region Helpers

    private void Resume()
    {
        wavePlayerManager.Init();
        if (playniteState.GamesPlaying is 0) 
        /* Then */ soundPlayer.Play(MainStateSettings().EnterSettings, musicPlayer.Resume);
    }

    private void Pause()
    {
        musicPlayer.Pause();
        if (playniteState.GamesPlaying is 0)
        /* Then */ soundPlayer.Play(MainStateSettings().ExitSettings, wavePlayerManager.WavePlayer.Pause);
    }

    private UIStateSettings MainStateSettings() => settings.ActiveModeSettings.UIStatesToSettings[UIState.Main];

    #endregion

    #endregion
}