using System;

namespace PlayniteSounds.Services.State;

public interface IAssemblyResolver : IDisposable
{
    IDisposable HandleAssemblies(params Type[] types);
}