using System;
using System.Collections.Generic;

namespace PlayniteSounds.Models;

public interface ISong
{
    string Album { get; set; }
    string Summary { get; }
    string Id { get; set; }
    string Name { get; set; }
    IEnumerable<string> Artists { get; set; }
    IEnumerable<string> Types { get; set; }
    string CreationDate { get; set; }
    IDictionary<string, string> Sizes { get; set; }
    TimeSpan? Length { get; set; }
    string Description { get; set; }
    string IconUri { get; set; }
    Source Source { get; set; }
    IEnumerable<string> Developers { get; set; }
    IEnumerable<string> Publishers { get; set; }
}