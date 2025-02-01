using System;

namespace PlayniteSounds.Common.Constants;

public static class App
{
    public const string AppName = "Playnite Sounds";
    public const string SourceName = "Sounds";
    public const string MainMenuName = "@" + AppName;
    public const string AppGuid = "9c960604-b8bc-4407-a4e4-e291c6097c7d";
    public const string ExtraMetaGuid = "705fdbca-e1fc-4004-b839-1d040b8b4429";
    public static readonly Lazy<string> HelpMessage = new(() =>
        Resource.MsgHelp1 + "\n\n" +
        Resource.MsgHelp2 + "\n\n" +
        Resource.MsgHelp3 + " " +
        Resource.MsgHelp4 + " " +
        Resource.MsgHelp5 + "\n\n" +
        Resource.MsgHelp6 + "\n\n" +
        HelpLine(SoundFile.BaseApplicationStartedSound) +
        HelpLine(SoundFile.BaseApplicationStoppedSound) +
        HelpLine(SoundFile.BaseGameInstalledSound) +
        HelpLine(SoundFile.BaseGameSelectedSound) +
        HelpLine(SoundFile.BaseGameStartedSound) +
        HelpLine(SoundFile.BaseGameStartingSound) +
        HelpLine(SoundFile.BaseGameStoppedSound) +
        HelpLine(SoundFile.BaseGameUninstalledSound) +
        HelpLine(SoundFile.BaseLibraryUpdatedSound) +
        Resource.MsgHelp7);
    private static string HelpLine(string baseMessage)
        => $"{SoundFile.DesktopPrefix}{baseMessage} - {SoundFile.FullScreenPrefix}{baseMessage}\n";
}