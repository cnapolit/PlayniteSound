using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using System;
using System.Diagnostics;
using System.IO;
using PlayniteSounds.Models;

namespace PlayniteSounds.Services.Files
{
    public class Normalizer : INormalizer
    {
        #region Infrastructure

        private static readonly ILogger                Logger    = LogManager.GetLogger();
        private        readonly PlayniteSoundsSettings _settings;


        public Normalizer(PlayniteSoundsSettings settings) => _settings = settings;

        #endregion

        #region Implementation

        #region NormalizeAudioFile

        public bool NormalizeAudioFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(_settings.FFmpegNormalizePath))
            {
                throw new ArgumentException("FFmpeg-Normalize path is undefined");
            }

            if (!File.Exists(_settings.FFmpegNormalizePath))
            {
                throw new ArgumentException("FFmpeg-Normalize file does not exist");
            }

            var args = SoundFile.DefaultNormArgs;
            if (!string.IsNullOrWhiteSpace(_settings.FFmpegNormalizeArgs))
            {
                args = _settings.FFmpegNormalizeArgs;
                Logger.Info($"Using custom args '{args}' for file '{filePath}' during normalization.");
            }


            var info = new ProcessStartInfo
            {
                Arguments = $"{args} \"{filePath}\" -o \"{filePath}\" -f",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = _settings.FFmpegNormalizePath
            };

            info.EnvironmentVariables["FFMPEG_PATH"] = _settings.FFmpegPath;

            var stdout = string.Empty;
            var stderr = string.Empty;
            using (var proc = new Process())
            {
                proc.StartInfo = info;
                proc.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stdout += e.Data + Environment.NewLine;
                    }
                };

                proc.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stderr += e.Data + Environment.NewLine;
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    Logger.Error($"FFmpeg-Normalize failed for file '{filePath}' with error: {stderr} and output: {stdout}");
                    return false;
                }

                Logger.Info($"FFmpeg-Normalize succeeded for file '{filePath}.");
                return true;
            }
        }

        #endregion

        #endregion
    }
}
