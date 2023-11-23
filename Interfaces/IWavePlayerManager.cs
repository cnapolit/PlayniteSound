using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace PlayniteSounds.Services.Audio
{
    public interface IWavePlayerManager : IDisposable
    {
        IWavePlayer WavePlayer { get; }
        MixingSampleProvider Mixer { get; }
        void Init();
    }
}