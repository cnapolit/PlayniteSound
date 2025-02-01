using NAudio.Wave;
using System.Collections.Generic;
using System.Linq;

namespace PlayniteSounds.Models.Audio;

internal class CachedSound
{
    public WaveFormat WaveFormat { get; }
    public float[] AudioData { get; }
    public float Volume { get; }

    public CachedSound(string audioFile, float volume)
    {
        Volume = volume;
        using var audioFileReader = new AudioFileReader(audioFile) { Volume = volume };
        WaveFormat = audioFileReader.WaveFormat;
        var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
        var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
        int samplesRead;
        while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
        {
            wholeFile.AddRange(readBuffer.Take(samplesRead));
        }
        AudioData = wholeFile.ToArray();
    }
}