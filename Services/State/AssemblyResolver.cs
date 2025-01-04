using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PlayniteSounds.Services.State
{
    public class AssemblyResolver : IAssemblyResolver
    {
        private readonly ILogger                              _logger;
        private readonly IDictionary<string, (int, Assembly)> _assemblies   = new Dictionary<string, (int, Assembly)>();
        private readonly object                               _assemblyLock = new object();
        private          bool                                 _disposed;

        public AssemblyResolver(ILogger logger) => _logger = logger;

        public IDisposable HandleAssemblies(params Assembly[] assemblies)
            => HandleAssemblies((ICollection <Assembly>)assemblies);

        public IDisposable HandleAssemblies(ICollection<Assembly> assemblies)
        {
            lock (_assemblyLock)
            {
                var newAssembly = false;
                var newAssemblies = new Dictionary<string, Assembly>();
                foreach (var assembly in assemblies)
                {
                    var assemblyName = assembly.GetName().Name;
                    if (_assemblies.TryGetValue(assemblyName, out var assemblyState))
                    {
                        if (assemblyState.Item1 is 0)
                        {
                            _logger.Info($"Assembly '{assemblyName}' was already handled");
                            continue;
                        }

                        _logger.Info($"Incrementing count '{assemblyState.Item1++}' for assembly {assemblyName}");
                    }
                    else
                    {
                        _logger.Info($"Adding assembly {assemblyName}");
                        _assemblies[assemblyName] = (1, assembly);
                        newAssembly = true;
                    }
                    newAssemblies[assemblyName] = assembly;
                }

                if (newAssemblies.Count is 0) /* Then */ return new DoNothingDisposable();

                if (newAssembly) /* Then */ AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

                return new AssemblyResolverHandle(this, newAssemblies);

            }
        }

        private Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            lock (_assemblyLock)
            {
                var nameToAssembly = _assemblies.FirstOrDefault(p => args.Name.StartsWith(p.Key));
                if (nameToAssembly.Key is null)
                {
                    _logger.Info($"Unable to resolve assembly '{args.Name}'");
                    return null;
                }

                _logger.Info($"Resolved assembly '{args.Name}'");
                return nameToAssembly.Value.Item2;
            }
        }

        private void RemoveAssemblies(IDictionary<string, Assembly> assemblies)
        {
            lock (_assemblyLock)
            {
                foreach (var assembly in assemblies)
                /* Then */ if (_assemblies.TryGetValue(assembly.Key, out var assemblyState))
                {
                    --assemblyState.Item1;
                }

                if (_assemblies.Count(a => a.Value.Item1 > 0) is 0)
                {
                    _logger.Info("No more assemblies to handle");
                    AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) /* Then */ return;
            _disposed = true;

            if (_assemblies.Count(a => a.Value.Item1 > 0) != 0)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
            }
        }

        private class DoNothingDisposable : IDisposable { public void Dispose() { } }

        private class AssemblyResolverHandle : IDisposable
        {
            private readonly AssemblyResolver _assemblyResolver;
            private readonly IDictionary<string, Assembly> _assemblies;
            private bool _disposed;

            public AssemblyResolverHandle(AssemblyResolver assemblyResolver, IDictionary<string, Assembly> assemblies)
            {
                _assemblyResolver = assemblyResolver;
                _assemblies = assemblies;
            }

            public void Dispose()
            {
                if (_disposed) /* Then */ return;
                _disposed = true;
                _assemblyResolver.RemoveAssemblies(_assemblies);
            }
        }
    }
}