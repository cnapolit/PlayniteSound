using System.IO;

namespace PlayniteSounds.Models.Audio.SampleProviders
{
    internal class WebStreamReader : IStreamReader
    {
        private readonly Stream _stream;

        public WebStreamReader(Stream stream) { _stream = stream; }

        public long Position { get => _stream.Position; set => _stream.Position = value; }

        public long Length => _stream.Length;

        public string FileName => null;

        public void Dispose() { /* stream should be disposed by the creator */ }
    }
}
