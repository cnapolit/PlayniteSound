using NAudio.Wave;
using System;

namespace PlayniteSounds.Models.Audio.SampleProviders
{
    class AutoDisposeFileReader : ISampleProvider, IDisposable
    {
        public WaveFormat WaveFormat => _reader.WaveFormat;

        private readonly AudioFileReader _reader;
        private bool _isDisposed;

        public AutoDisposeFileReader(string fileName, float volume)
            => _reader = new AudioFileReader(fileName) { Volume = volume };

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
}
