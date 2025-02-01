using Playnite.SDK;
using System;
using System.Collections.Generic;

namespace PlayniteSounds.Services;

public interface IPromptFactory
{
    void CreateGlobalProgress(string progressSubTitle, Action<GlobalProgressActionArgs, string> action);
    T PromptForSelect<T>(string captionFormat, string formatArg, Func<string, List<GenericItemOption>> search, string defaultSearch);
    IEnumerable<string> PromptForMp3();
    IEnumerable<string> PromptForAudioFile();
    bool PromptForApproval(string caption);
    void ShowError(string error);
    void ShowMessage(string message);
}