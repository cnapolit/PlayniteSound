using PlayniteSounds.GeneratedFactories;
using System;

namespace PlayniteSounds.Services
{
    public class FactoryExecutor<T> : IFactoryExecutor<T>
    {
        private readonly IFactory<T> _factory;

        public FactoryExecutor(IFactory<T> factory) => _factory = factory;

        public void Execute(Action<T> action)
        {
            var component = _factory.Create();
            action(component);
            _factory.Release(component);
        }

    }
}
