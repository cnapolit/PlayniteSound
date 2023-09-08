using System;
using System.Collections.Generic;
using System.Reflection;

namespace PlayniteSounds.Services.State 
{
    public interface IAssemblyResolver : IDisposable
    {
        bool IsResolvingAssembles { get; }
        IDisposable HandleAssembly(string assemblyName, Assembly assembly);
        IDisposable HandleAssemblies(IDictionary<string, Assembly> assemblies);
    }
}