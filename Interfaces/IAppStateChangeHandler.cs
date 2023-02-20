using Microsoft.Win32;
using System;

namespace PlayniteSounds.Services.State
{
    public interface IAppStateChangeHandler
    {
        void OnApplicationActivate(object sender, EventArgs e);
        void OnApplicationDeactivate(object sender, EventArgs e);
        void OnPowerModeChanged(object sender, PowerModeChangedEventArgs args);
        void OnWindowStateChanged(object sender, EventArgs e);
    }
}