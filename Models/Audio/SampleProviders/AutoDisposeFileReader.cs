using NAudio.Wave;
using System;

namespace PlayniteSounds.Models.Audio.SampleProviders;

class AutoDisposeFileReader(string fileName, float volume) : ISampleProvider, IDisposable
{
    public WaveFormat WaveFormat => _reader.WaveFormat;

    private readonly AudioFileReader _reader = new(fileName) { Volume = volume };
    private bool _isDisposed;

    ~AutoDisposeFileReader() => Dispose();

    public int Read(float[] buffer, int offset, int count)
    {
        if (_isDisposed)
        {
            return 0;
        }

        var read = _reader.Read(buffer, offset, count);
        if (read != count)
        {
            Dispose();
        }
        return read;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _reader.Dispose();
        _isDisposed = true;
    }
}