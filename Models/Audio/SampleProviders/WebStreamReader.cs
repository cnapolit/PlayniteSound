using System;
using NAudio.Wave;
using System.IO;

namespace PlayniteSounds.Models.Audio.SampleProviders
{
    internal class WebStreamReader : IStreamReader
    {
        private readonly WaveStream _stream;

        public WebStreamReader(WaveStream stream) { _stream = stream; }

        public TimeSpan CurrentTime => _stream.CurrentTime;
        public TimeSpan TotalTime => _stream.TotalTime;

        public long Position { get => _stream.Position; set => _stream.Position = value; }

        public long Length => _stream.Length;

        public string FileName => null;

        public void Dispose() { /* stream should be disposed by the creator */ }
    }
}
