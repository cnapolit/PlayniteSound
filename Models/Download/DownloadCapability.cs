using System;

namespace PlayniteSounds.Models.Download;

[Flags]
public enum DownloadCapabilities
{
    None = 0,
    Bulk = 1,
    Album = 2,
    Infinite = 4,
    Batching = 8,
    FlatSearch = 16
}