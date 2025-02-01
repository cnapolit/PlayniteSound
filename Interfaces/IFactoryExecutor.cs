using System;

namespace PlayniteSounds.Services;

public interface IFactoryExecutor<out T>
{
    void Execute(Action<T> action);
}