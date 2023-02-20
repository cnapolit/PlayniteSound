using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using System;
using System.Diagnostics;

namespace PlayniteSounds.Services.State
{
    public class ErrorHandler : IErrorHandler
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

        #region Try(Action action)

        public void Try(Action action) { try { action(); } catch (Exception ex) { HandleException(ex); } }

        #endregion

        #region Try<T>(Action action)

        public void Try<T>(Action<T> action, T arg)
        {
            try { action(arg); } 
            catch (Exception ex) { HandleException(ex); } 
        }

        #endregion

        #region Try<T>(Func action)

        public T Try<T>(Func<T> action)
        { 
            try { return action(); } 
            catch (Exception ex) { HandleException(ex); } 
            return default;
        }

        #endregion

        #region Try(Action action, Action final)

        public void Try(Action action, Action final) 
        { try { action(); } catch (Exception ex) { HandleException(ex); } finally { final(); } }

        #endregion

        #endregion
    }
}
