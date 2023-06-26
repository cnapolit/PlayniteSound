using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using System;
using System.Diagnostics;
using System.IO;
using PlayniteSounds.Models;
using System.Collections.Generic;
using System.Linq;
using PlayniteSounds.Services.Audio;
using Playnite.SDK.Models;
using PlayniteSounds.Common.Extensions;
using PlayniteSounds.Services.Play;
using PlayniteSounds.Common.Utilities;

namespace PlayniteSounds.Services.Files
{
    public class Normalizer : INormalizer
    {
        #region Infrastructure

        private readonly IMainViewAPI _mainViewAPI;
        private readonly IPromptFactory _promptFactory;
        private readonly IMusicPlayer _musicPlayer;
        private readonly IPathingService _pathingService;
        private readonly ITagger _tagger;
        private readonly ILogger _logger;
        private readonly PlayniteSoundsSettings _settings;


        public Normalizer(
            IMainViewAPI mainViewAPI,
            IPromptFactory promptFactory,
            IMusicPlayer musicPlayer,
            IPathingService pathingService,
            ITagger tagger,
            ILogger logger,
            PlayniteSoundsSettings settings)
        {
            _mainViewAPI = mainViewAPI;
            _promptFactory = promptFactory;
            _musicPlayer = musicPlayer;
            _pathingService = pathingService;
            _tagger = tagger;
            _logger = logger;
            _settings = settings;
        }

        #endregion

        #region Implementation

        #region CreateNormalizationDialogue

        public void CreateNormalizationDialogue()
        {
            var failedGames = new List<string>();

            void NormalizeGames(GlobalProgressActionArgs args, string title)
                => failedGames = NormalizeSelectedGameMusicFiles(args, _mainViewAPI.SelectedGames.ToList(), title);

            _promptFactory.CreateGlobalProgress(Resource.DialogMessageNormalizingFiles, NormalizeGames);

            if (failedGames.Any())
            {
                _promptFactory.ShowError($"The following games had at least one file fail to normalize (see logs for details): {string.Join(", ", failedGames)}");
            }
            else
            {
                _promptFactory.ShowMessage(Resource.DialogMessageDone);
            }

            _musicPlayer.Play(_mainViewAPI.SelectedGames);
        }

        private List<string> NormalizeSelectedGameMusicFiles(
            GlobalProgressActionArgs args, IList<Game> games, string progressTitle)
        {
            var failedGames = new List<string>();
            var gamesToUpdate = new List<Game>();

            args.ProgressMaxValue = games.Count;
            foreach (var game in games.TakeWhile(_ => !args.CancelToken.IsCancellationRequested))
            {
                args.Text = UIUtilities.GenerateTitle(args, game, progressTitle);

                var musicFiles = _pathingService.GetGameMusicFiles(game);
                if (musicFiles.IsEmpty())
                {
                    continue;
                }

                var allMusicNormalized = musicFiles.ForAny(NormalizeAudioFile);
                if (allMusicNormalized)
                {
                    _tagger.AddTag(game, Resource.NormTag);
                    gamesToUpdate.Add(game);
                }
                else
                {
                    failedGames.Add(game.Name);
                }
            }

            if (gamesToUpdate.Any())
            {
                args.Text = progressTitle + "Updating Tags";

            }
            return failedGames;
        }

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
                _logger.Info($"Using custom args '{args}' for file '{filePath}' during normalization.");
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
                    _logger.Error($"FFmpeg-Normalize failed for file '{filePath}' with error: {stderr} and output: {stdout}");
                    return false;
                }

                _logger.Info($"FFmpeg-Normalize succeeded for file '{filePath}.");
                return true;
            }
        }

        #endregion

        #endregion
    }
}
