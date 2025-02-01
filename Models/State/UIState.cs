using System;

namespace PlayniteSounds.Models;

[Flags]
public enum UIState
{
    Main,
    GameDetails,
    MainMenu,
    Filters = 4,
    FilterPresets = 8,
    Search = 16,
    Settings = 32,
    GameMenu = 64,
    Notifications = 128,
    GameMenu_GameDetails = GameMenu | GameDetails
}