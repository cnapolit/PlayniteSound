using NAudio.Wave;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PlayniteSounds.Services.Audio
{
    public class StartSoundSelector
    {
        public enum SelectStartAlgorithm
        {
            All,
            Start,
            End,
            Snip,
            StartTrimSilence,
            EndTrimSilence
        }

        public (int Start, int End) SelectStartSound(string filePath, SelectStartAlgorithm selectStartAlgorithm)
        {
            const int DefaultStart = 7;
            const int DefaultEnd = 7;

            using (var reader = new AudioFileReader(filePath))
            {
                FindClip(reader);
                return (0, 0);
                var start = 0;
                var end = (int)reader.Length;
                switch (selectStartAlgorithm)
                {
                    case SelectStartAlgorithm.StartTrimSilence:
                        start = TrimStartSilence(reader);
                        goto case SelectStartAlgorithm.Start;
                    case SelectStartAlgorithm.Start:
                        end = start + reader.WaveFormat.AverageBytesPerSecond * DefaultEnd;
                        break;
                    case SelectStartAlgorithm.EndTrimSilence:
                        end = TrimEndSilence(reader);
                        goto case SelectStartAlgorithm.End;
                    case SelectStartAlgorithm.End:
                        start = end - DefaultStart * reader.WaveFormat.AverageBytesPerSecond;
                        break;
                    case SelectStartAlgorithm.Snip:
                        break;
                }
                return (start, end);
            }
        }

        public static int TrimStartSilence(AudioFileReader reader)
        {
            const int decibelThreshold = -40;

            var bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;
            var bufferSize = bytesPerMillisecond / sizeof(float);
            var buffer = new float[bufferSize];

            var totalSamplesRead = 0;
            var samplesRead = 1;
            while (samplesRead != 0)
            {
                samplesRead = reader.Read(buffer, 0, bufferSize);
                for (var i = 0; i < samplesRead; i++)
                {
                    if (NAudio.Utils.Decibels.LinearToDecibels(buffer[i]) <= decibelThreshold) /* Then */ continue;

                    totalSamplesRead += i * reader.WaveFormat.Channels;
                    samplesRead = 0;
                    break;
                }

                totalSamplesRead += samplesRead;
            }

            var bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
            return totalSamplesRead * bytesPerSample;       
        }

        public static int TrimEndSilence(AudioFileReader reader)
        {
            const int decibelThreshold = -40;

            var bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;
            var bufferSize = bytesPerMillisecond / sizeof(float);
            var buffer = new float[bufferSize];

            // initial starting point set such that the final position will land on zero when fully traversing file
            var newPosition = reader.Length - reader.Length % bytesPerMillisecond;
            var totalSamplesRead = -1;
            while (newPosition < 0)
            {
                reader.Position = newPosition;
                var samplesRead = reader.Read(buffer, 0, bytesPerMillisecond);
                newPosition -= bytesPerMillisecond;

                for (var i = samplesRead; i > -1; i--)
                {
                    if (NAudio.Utils.Decibels.LinearToDecibels(buffer[i]) < decibelThreshold) /* Then */ continue;

                    samplesRead = i * reader.WaveFormat.Channels;
                    newPosition = -1;
                    break;
                }
                totalSamplesRead += samplesRead;
            }

            var bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
            return (int)reader.Length - totalSamplesRead * bytesPerSample;
        }
        public static (int, int) FindClip(AudioFileReader reader)
        {
            const double SlopeThreshold = -5;
            var buffer = new float[reader.Length];

            var pointsOfChange = new List<(int, double)>();

            var samplesRead = reader.Read(buffer, 0, (int)reader.Length);
            var n = reader.WaveFormat.SampleRate / 2;
            var nIndex = n * reader.WaveFormat.Channels;
            var sumOfXY = 0d;
            var sumOfX = 0d;
            var sumOfY = 0d;
            var sumOfXSquared = 0d;
            var yBuff = 10000000000d;
            for (var i = 0; i < nIndex; i += reader.WaveFormat.Channels)
            {
                var x = i / reader.WaveFormat.Channels;
                var y = Math.Abs(buffer[i]) * yBuff;
                sumOfXY += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSquared += x * x;
            }
            var state = ClipState.Point;
            var slope = CalculateSlope(n, sumOfXY, sumOfX, sumOfY, sumOfXSquared);
            if (slope < SlopeThreshold)
            {
                pointsOfChange.Add((0, slope));
                state = ClipState.End;
            }

            var posSlopeSum = 0d;
            var negSlopeSum = 0d;
            var posSlopes = new List<double>((samplesRead / reader.WaveFormat.Channels) - n);
            var posSlopesIndices = new List<int>((samplesRead / reader.WaveFormat.Channels) - n);
            var negSlopes = new List<double>((samplesRead / reader.WaveFormat.Channels) - n);
            var negSlopesIndices = new List<int>((samplesRead / reader.WaveFormat.Channels) - n);
            for (var i = nIndex; i < samplesRead; i += reader.WaveFormat.Channels)
            {
                var x = (i - nIndex) / reader.WaveFormat.Channels;
                var y = Math.Abs(buffer[i - nIndex]) * yBuff;
                sumOfXY -= x * y;
                sumOfX -= x;
                sumOfY -= y;
                sumOfXSquared -= x * x;

                var newX = i / reader.WaveFormat.Channels;
                var newY = Math.Abs(buffer[i]) * yBuff;
                sumOfXY += newX * newY;
                sumOfX += newX;
                sumOfY += newY;
                sumOfXSquared += newX * newX;

                slope = CalculateSlope(n, sumOfXY, sumOfX, sumOfY, sumOfXSquared);
                if (slope < 0)
                {
                    negSlopeSum += slope;
                    negSlopes.Add(slope);
                    negSlopesIndices.Add(i - nIndex + 1);
                }
                else
                {
                    posSlopeSum += slope;
                    posSlopes.Add(slope);
                    posSlopesIndices.Add(i - nIndex + 1);
                }
                switch (state)
                {
                    case ClipState.Point:
                        if (slope < SlopeThreshold)
                        {
                            pointsOfChange.Add((i - nIndex + 1, slope));
                            state = ClipState.End;
                        }
                        break;
                    case ClipState.Peak:
                        break;
                    case ClipState.End:
                        if (slope >= 0)
                        {
                            state = ClipState.Point;
                        }
                        break;
                }

            }
            return (0, 0);
        }

        private enum ClipState
        {
            Point,
            Peak,
            End
        }

        private static double CalculateSlope(int n, double sumOfXY, double sumOfX, double sumOfY, double sumOfXSquared)
            => (n * sumOfXY - sumOfX* sumOfY) / (n* sumOfXSquared - sumOfX * sumOfX);
    }
}


public class SampleProviderEnumerable : IEnumerable<float>
{
    private readonly ISampleProvider _sampleProvider;

    public int BufferSize { get; set; }

    public SampleProviderEnumerable(ISampleProvider sampleProvider)
    {
        _sampleProvider = sampleProvider;
        BufferSize = _sampleProvider.WaveFormat.AverageBytesPerSecond / 1000;
    }

    public IEnumerator<float> GetEnumerator() => new SampleProviderEnumerator(_sampleProvider, BufferSize);
    IEnumerator IEnumerable.GetEnumerator() => new SampleProviderEnumerator(_sampleProvider, BufferSize);

    private class SampleProviderEnumerator : IEnumerator<float>
    {
        private readonly ISampleProvider _sampleProvider;
        private readonly float[] _buffer;
        private readonly int _bufferSize;
        private int _position;
        private int _samplesRead;

        public SampleProviderEnumerator(ISampleProvider sampleProvider, int bufferSize)
        {
            _sampleProvider = sampleProvider;
            _buffer = new float[bufferSize];
            _bufferSize = bufferSize;
            _position = bufferSize;
        }

        public float Current => _buffer[_position];

        object IEnumerator.Current => _buffer[_position];

        public void Dispose() { }
        public bool MoveNext()
        {
            _position += _sampleProvider.WaveFormat.Channels;
            if (_position < _samplesRead)
            {
                return true;
            }

            _position = 0;
            _samplesRead = _sampleProvider.Read(_buffer, 0, _bufferSize);
            if (_samplesRead != 0)
            {
                return true;
            }

            return false;
        }
        public void Reset() => throw new NotImplementedException();
    }
}
