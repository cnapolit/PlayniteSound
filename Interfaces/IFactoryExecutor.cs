using System;

namespace PlayniteSounds.Services
{
    public interface IFactoryExecutor<T>
    {
        void Execute(Action<T> action);
    }
}