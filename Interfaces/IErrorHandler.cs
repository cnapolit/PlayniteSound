using System;

namespace PlayniteSounds.Services
{
    internal interface IErrorHandler
    {
        void HandleException(Exception e);
        void Try(Action action);
    }
}
