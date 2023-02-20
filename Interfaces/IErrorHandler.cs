using System;

namespace PlayniteSounds.Services
{
    public interface IErrorHandler
    {
        void HandleException(Exception e);
        void Try(Action action);
        void Try(Action action, Action final);
        T Try<T>(Func<T> action);
        void Try<T>(Action<T> action, T arg);
    }
}
