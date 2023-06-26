using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Models;
using System;
using System.Collections.Generic;
using System.Windows;

namespace PlayniteSounds.Services.UI
{
    public class PromptFactory : IPromptFactory
    {
        #region Infrastructure

        private readonly IDialogsFactory _dialogs;
        private readonly IErrorHandler   _errorHandler;

        public PromptFactory(IDialogsFactory dialogs, IErrorHandler errorHandler)
        {
            _dialogs = dialogs;
            _errorHandler = errorHandler;
        }

        #endregion

        #region Implementation

        #region PromptForSelect

        public T PromptForSelect<T>(
            string captionFormat,
            string formatArg,
            Func<string, List<GenericItemOption>> search,
            string defaultSearch)
        {
            var option = _dialogs.ChooseItemWithSearch(
                new List<GenericItemOption>(), search, defaultSearch, string.Format(captionFormat, formatArg));

            if (option is GenericObjectOption idOption && idOption.Object is T obj)
            {
                return obj;
            }

            return default;
        }

        #endregion

        #region PromptForApproval

        public bool PromptForApproval(string caption)
            => _dialogs.ShowMessage(caption, Resource.DialogCaptionSelectOption, MessageBoxButton.YesNo)
                is MessageBoxResult.Yes;

        #endregion

        #region ShowError

        public void ShowError(string error) => _dialogs.ShowErrorMessage(error, App.AppName);

        #endregion

        #region ShowMessage

        public void ShowMessage(string resource) => _dialogs.ShowMessage(resource, App.AppName);

        #endregion

        #region PromptForMp3

        public IEnumerable<string> PromptForMp3() => _dialogs.SelectFiles("Any|*.*") ?? new List<string>();

        #endregion

        #region PromptForAudioFile

        public IEnumerable<string> PromptForAudioFile() => _dialogs.SelectFiles("ALL|*.*") ?? new List<string>();

        #endregion

        #region CreateGlobalProgress

        public void CreateGlobalProgress(string progressSubTitle, Action<GlobalProgressActionArgs, string> action)
        {

            var progressTitle = $"{App.AppName} - {progressSubTitle}";
            var progressOptions = new GlobalProgressOptions(progressTitle, true) { IsIndeterminate = false };

            _dialogs.ActivateGlobalProgress(a => _errorHandler.TryWithPrompt(() => action(a, progressTitle)), progressOptions);
        }

        #endregion

        #endregion
    }
}
