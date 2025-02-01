using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Models;
using System;
using System.Collections.Generic;
using System.Windows;

namespace PlayniteSounds.Services.UI;

public class PromptFactory(IDialogsFactory dialogs, IErrorHandler errorHandler) : IPromptFactory
{
    #region Infrastructure

    #endregion

    #region Implementation

    #region PromptForSelect

    public T PromptForSelect<T>(
        string captionFormat,
        string formatArg,
        Func<string, List<GenericItemOption>> search,
        string defaultSearch)
    {
        var option = dialogs.ChooseItemWithSearch(
            [], search, defaultSearch, string.Format(captionFormat, formatArg));

        return option is GenericObjectOption { Object: T obj } ? obj : default;
    }

    #endregion

    #region PromptForApproval

    public bool PromptForApproval(string caption)
        => dialogs.ShowMessage(caption, Resource.DialogCaptionSelectOption, MessageBoxButton.YesNo)
            is MessageBoxResult.Yes;

    #endregion

    #region ShowError

    public void ShowError(string error) => dialogs.ShowErrorMessage(error, App.AppName);

    #endregion

    #region ShowMessage

    public void ShowMessage(string resource) => dialogs.ShowMessage(resource, App.AppName);

    #endregion

    #region PromptForMp3

    public IEnumerable<string> PromptForMp3() => dialogs.SelectFiles("Any|*.*") ?? [];

    #endregion

    #region PromptForAudioFile

    public IEnumerable<string> PromptForAudioFile() => dialogs.SelectFiles("ALL|*.*") ?? [];

    #endregion

    #region CreateGlobalProgress

    public void CreateGlobalProgress(string progressSubTitle, Action<GlobalProgressActionArgs, string> action)
    {
        var progressTitle = $"{App.AppName} - {progressSubTitle}";
        var progressOptions = new GlobalProgressOptions(progressTitle, true) { IsIndeterminate = false };

        dialogs.ActivateGlobalProgress(
            a => errorHandler.TryWithPrompt(() => action(a, progressTitle)), progressOptions);
    }

    #endregion

    #endregion
}