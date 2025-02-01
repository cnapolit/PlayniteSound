using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using System;
using System.Diagnostics;
using System.IO;
using PlayniteSounds.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlayniteSounds.Services.Audio;
using Playnite.SDK.Models;
using PlayniteSounds.Common.Extensions;
using PlayniteSounds.Services.Play;
using PlayniteSounds.Common.Utilities;
using System.Threading.Tasks;

namespace PlayniteSounds.Services.Files;

public class Normalizer(
    IMainViewAPI mainViewApi,
    IPromptFactory promptFactory,
    IMusicPlayer musicPlayer,
    IPathingService pathingService,
    ITagger tagger,
    ILogger logger,
    PlayniteSoundsSettings settings)
    : INormalizer
{
    #region Implementation

    #region CreateNormalizationDialogue

    public void CreateNormalizationDialogue()
    {
        var failedGames = new List<string>();

        void NormalizeGames(GlobalProgressActionArgs args, string title)
            => failedGames = NormalizeSelectedGameMusicFiles(args, mainViewApi.SelectedGames.ToList(), title);

        promptFactory.CreateGlobalProgress(Resource.DialogMessageNormalizingFiles, NormalizeGames);

        if (failedGames.Any())
        {
            promptFactory.ShowError($"The following games had at least one file fail to normalize (see logs for details): {string.Join(", ", failedGames)}");
        }
        else
        {
            promptFactory.ShowMessage(Resource.DialogMessageDone);
        }
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

            var musicFiles = pathingService.GetGameMusicFiles(game);
            if (musicFiles.IsEmpty())
            {
                continue;
            }

            var allMusicNormalized = musicFiles.ForAny(NormalizeAudioFile);
            if (allMusicNormalized)
            {
                tagger.AddTag(game, Resource.NormTag);
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
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        using var proc = CreateProcess(filePath, stdOut, stdErr);
        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();

        if (stdErr.Length > 0)
        {
            logger.Error($"FFmpeg-Normalize failed for file '{filePath}' with error: {stdErr} and output: {stdOut}");
            return false;
        }

        logger.Info($"FFmpeg-Normalize succeeded for file '{filePath}.");
        return true;
    }

    public async Task<bool> NormalizeAudioFileAsync(string filePath)
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        using var proc = CreateProcess(filePath, stdOut, stdErr);
        var tcs = new TaskCompletionSource<int>();

        proc.Exited += (_, _) => tcs.SetResult(proc.ExitCode);

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        await tcs.Task;
        proc.WaitForExit();

        if (stdErr.Length > 0)
        {
            logger.Error($"FFmpeg-Normalize failed for file '{filePath}' with error: {stdErr} and output: {stdOut}");
            return false;
        }

        logger.Info($"FFmpeg-Normalize succeeded for file '{filePath}.");
        return true;
    }

    private Process CreateProcess(string filePath, StringBuilder stdOut, StringBuilder stdErr)
    {
        if (string.IsNullOrWhiteSpace(settings.FFmpegNormalizePath))
        {
            throw new ArgumentException("FFmpeg-Normalize path is undefined");
        }

        if (!File.Exists(settings.FFmpegNormalizePath))
        {
            throw new ArgumentException("FFmpeg-Normalize file does not exist");
        }

        var args = SoundFile.DefaultNormArgs;
        if (!string.IsNullOrWhiteSpace(settings.FFmpegNormalizeArgs))
        {
            args = settings.FFmpegNormalizeArgs;
            logger.Info($"Using custom args '{args}' for file '{filePath}' during normalization.");
        }


        var info = new ProcessStartInfo
        {
            Arguments = $"{args} \"{filePath}\" -o \"{filePath}\" -f",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            UseShellExecute = false,
            FileName = settings.FFmpegNormalizePath
        };

        info.EnvironmentVariables["FFMPEG_PATH"] = settings.FFmpegPath;

        var proc = new Process() { StartInfo = info, };
        proc.StartInfo = info;
        proc.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stdOut.Append(e.Data);
            }
        };

        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stdErr.Append(e.Data);
            }
        };
        return proc;
    }

    #endregion

    #endregion
}