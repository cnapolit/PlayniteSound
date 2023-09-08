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
        private readonly IDictionary<string, (int, Assembly)> _assemblies = new Dictionary<string, (int, Assembly)>();
        private readonly object                               _assemblyLock = new object();
        private bool                                          _disposed;

        public bool IsResolvingAssembles => _assemblies.Count > 0;

        public AssemblyResolver(ILogger logger) => _logger = logger;

        public IDisposable HandleAssembly(string assemblyName, Assembly assembly)
            => HandleAssemblies(new Dictionary<string, Assembly> { [assemblyName] = assembly });

        public IDisposable HandleAssemblies(IDictionary<string, Assembly> assemblies)
        {
            lock (_assemblyLock)
            {
                var initialAssemblyCount = _assemblies.Count;

                foreach (var assembly in assemblies)
                {
                    if (_assemblies.TryGetValue(assembly.Key, out var assemblyState))
                    {
                        _logger.Info($"Incrementing count '{assemblyState.Item1++}' for assembly {assembly.Key}");
                    }
                    else
                    {
                        _logger.Info($"Adding assembly {assembly.Key}");
                        _assemblies[assembly.Key] = (1, assembly.Value);
                    }
                }

                // Only Add handler upon successful assembly addition
                if (initialAssemblyCount is 0)
                {
                    _logger.Info($"Adding handler with assemblies '{string.Join(", ", assemblies.Select(p => p.Key))}'");
                    AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
                }

                return new AssemblyResolverHandle(this, assemblies);
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
                var assemblieswasNotEmpty = _assemblies.Count > 0;

                if (!assemblieswasNotEmpty) /* Then */ return;

                var currentAssembly = string.Empty;
                try
                {
                    foreach (var assembly in assemblies)
                    {
                        currentAssembly = assembly.Key;
                        if (_assemblies.TryGetValue(assembly.Key, out var assemblyState))
                        {
                            assemblyState.Item1--;
                            if (assemblyState.Item1 < 1)
                            {
                                _assemblies.Remove(assembly.Key);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Failed to remove assembly '{currentAssembly}'. Dumping all assemblies & removing handler.");
                    if (assemblieswasNotEmpty)
                    {
                        AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
                    }
                    _assemblies.Clear();
                    throw e;
                }

                if (assemblieswasNotEmpty && _assemblies.Count is 0)
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) /* Then */ return;
            _disposed = true;

            if (_assemblies.Count != 0)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
            }
        }

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