using PlayniteSounds.GeneratedFactories;
using System;

namespace PlayniteSounds.Services;

public class FactoryExecutor<T>(IFactory<T> factory) : IFactoryExecutor<T>
{
    public void Execute(Action<T> action)
    {
        var component = factory.Create();
        action(component);
        factory.Release(component);
    }

}