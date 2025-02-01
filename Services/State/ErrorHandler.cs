using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using System;
using System.Diagnostics;

namespace PlayniteSounds.Services.State;

public class ErrorHandler(IDialogsFactory dialogsFactory, ILogger logger) : IErrorHandler
{
    #region Infrastructure

    #endregion

    #region Implementation

    #region LogException
    public void LogException(Exception e)
        => logger.Error(e, new StackTrace(e).GetFrame(0).GetMethod().Name);

    #endregion

    #region CreateExceptionPrompt

    public void CreateExceptionPrompt(Exception e)
    {
        LogException(e);
        dialogsFactory.ShowErrorMessage(e.Message, App.AppName);
    }

    #endregion

    #region TryWithPrompt(Action action)

    public void TryWithPrompt(Action action) 
    { try { action(); } catch (Exception ex) { CreateExceptionPrompt(ex); } }

    #endregion

    #region TryWithPrompt<T>(Action action)

    public void TryWithPrompt<T>(Action<T> action, T arg)
    { try { action(arg); } catch (Exception ex) { CreateExceptionPrompt(ex); }  }

    #endregion

    #region TryWithPrompt<T>(Func action)

    public T TryWithPrompt<T>(Func<T> action)
    { try { return action(); } catch (Exception ex) { CreateExceptionPrompt(ex); } return default; }

    #endregion

    #region TryWithPrompt(Action action, Action final)

    public void TryWithPrompt(Action action, Action final) 
    { try { action(); } catch (Exception ex) { CreateExceptionPrompt(ex); } finally { final(); } }

    #endregion

    #region Try(Action action)

    public void Try(Action action)
    { try { action(); } catch (Exception ex) { LogException(ex); } }

    #endregion

    #region Try<T>(Action action)

    public void Try<T>(Action<T> action, T arg)
    { try { action(arg); } catch (Exception ex) { LogException(ex); } }

    #endregion

    #region Try<T>(Func action)

    public T Try<T>(Func<T> action)
    {
        try { return action(); } catch (Exception ex) { LogException(ex); } return default;
    }

    #endregion

    #region Try(Action action, Action final)

    public void Try(Action action, Action final)
    { try { action(); } catch (Exception ex) { LogException(ex); } finally { final(); } }

    #endregion

    #endregion
}