using System;

namespace PlayniteSounds.Services;

public interface IErrorHandler
{
    void CreateExceptionPrompt(Exception e);
    void LogException(Exception e);
    void Try(Action action);
    void Try(Action action, Action final);
    void Try<T>(Action<T> action, T arg);
    T Try<T>(Func<T> action);
    void TryWithPrompt(Action action);
    void TryWithPrompt(Action action, Action final);
    T TryWithPrompt<T>(Func<T> action);
    void TryWithPrompt<T>(Action<T> action, T arg);
}