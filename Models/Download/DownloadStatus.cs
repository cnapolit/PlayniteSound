using System;

namespace PlayniteSounds.Models.Download;

[Flags]
public enum DownloadStatus
{
    Failed,
    Downloaded,
    Normalized
}