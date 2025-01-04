using NAudio.Wave;

namespace PlayniteSounds.Models.Audio.SampleProviders
{
    internal class AudioFileStreamReader : IStreamReader
    {
        private readonly AudioFileReader _reader;

        public AudioFileStreamReader(AudioFileReader reader) => _reader = reader;

        public long Position { get => _reader.Position; set => _reader.Position = value; }

        public long Length => _reader.Length;

        public string FileName => _reader.FileName;

        public void Dispose() => _reader.Dispose();
    }
}
