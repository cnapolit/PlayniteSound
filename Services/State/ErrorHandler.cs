using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using System;
using System.Diagnostics;

namespace PlayniteSounds.Services.State
{
    internal class ErrorHandler : IErrorHandler
    {
        #region Infrastructure

        private static readonly ILogger      logger = LogManager.GetLogger();
        private        readonly IPlayniteAPI _api;

        public ErrorHandler(IPlayniteAPI api) => _api = api;

        #endregion

        #region Implementation

        #region HandleException

        public void HandleException(Exception e)
        {
            logger.Error(e, new StackTrace(e).GetFrame(0).GetMethod().Name);
            _api.Dialogs.ShowErrorMessage(e.Message, App.AppName);
        }

        #endregion

        #region Try

        public void Try(Action action) { try { action(); } catch (Exception ex) { HandleException(ex); } }

        #endregion

        #endregion
    }
}
