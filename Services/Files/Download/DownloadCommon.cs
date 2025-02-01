using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteSounds.Files.Download;

public static class DownloadCommon
{
    public static async Task DownloadStreamAsync(
        Stream sourceStream, string filePath, IProgress<double> progress, CancellationToken token)
    {
        using var fileStream = File.Create(filePath);
        await DownloadStreamAsync(sourceStream, fileStream, progress, token);
    }

    public static async Task DownloadStreamAsync(
        Stream sourceStream, Stream targetStream, IProgress<double> progress, CancellationToken token)
    {
        var buffer = new byte[81920];
        var totalBytes = sourceStream.Length;
        long totalBytesCopied = 0;
        int bytesRead;

        while (!token.IsCancellationRequested
            && (bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
        {
            await targetStream.WriteAsync(buffer, 0, bytesRead, token);
            progress.Report((double)(totalBytesCopied += bytesRead) / totalBytes);
        }
    }
}