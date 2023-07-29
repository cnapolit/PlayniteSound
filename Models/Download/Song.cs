﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace PlayniteSounds.Models
{
    public class Song : DownloadItem
    {
        public string Description { get; set; }
        public string SizeInMb { get; set; }
        public TimeSpan? Length { get; set; }
        protected override IEnumerable<PropertyInfo> Properties => typeof(Song).GetProperties();
    }
}
