using System;
using System.Collections.Generic;
using System.Reflection;

namespace PlayniteSounds.Services.State 
{
    public interface IAssemblyResolver : IDisposable
    {
        IDisposable HandleAssemblies(params Assembly[] assemblies);
        IDisposable HandleAssemblies(ICollection<Assembly> assemblies);
    }
}