using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlayniteSounds.Common.Extensions;
using System.Text.RegularExpressions;

namespace PlayniteSounds.Models
{
    public abstract class BaseItem
    {
        public string                      Id           { get; set; }
        public string                      Name         { get; set; }
        public ICollection<string>         Artists      { get; set; }
        public ICollection<string>         Types        { get; set; }
        public string                      CreationDate { get; set; }
        public IDictionary<string, string> Sizes        { get; set; }
        public TimeSpan?                   Length       { get; set; }
        public string                      Description  { get; set; }
        public string                      CoverUri     { get; set; }
        public string                      IconUri      { get; set; }
        public Source                      Source       { get; set; }
        public uint?                       TrackNumber  { get; set; }

        public abstract string Summary { get; }

        private static readonly IList<string> Properties = new[]
        {
            nameof(Name),
            nameof(CreationDate),
            nameof(Artists),
            nameof(Length),
            nameof(Types),
            nameof(Sizes),
            nameof(TrackNumber)
        };

        protected virtual IList<string> GetProperties() => Properties;

        public override string ToString() => JoinProperties(PropertiesToStrings(Properties));

        public IEnumerable<Tuple<string, string>> PropertiesToValues => PropertiesToPairs(GetProperties());

        protected IEnumerable<Tuple<string, string>> PropertiesToPairs(IEnumerable<string> properties) => PropertiesToPairs(properties, GetType());
        private IEnumerable<Tuple<string, string>> PropertiesToPairs(IEnumerable<string> properties, Type type) =>
            from property in properties
            let propertyValue = type.GetProperty(property).GetValue(this)
            let value = GetDisplayNameAndValue(FormatName(property), propertyValue)
            where value != null
            select value;

        protected Tuple<string, string> ListableEntryToPair(ICollection<string> entry, string plural, string singular = null)
        {
            switch (entry?.Count)
            {
                case null:
                case 0: return null;
                case 1: return new Tuple<string, string>(singular ?? plural.Substring(0, plural.Length - 1), entry.First());
                default: return new Tuple<string, string>(plural, JoinProperties(entry));
            }
        }

        private string ListableEntryToString<T>(ICollection<T> entry, string singular, string plural)
        {
            switch (entry?.Count)
            {
                case null:
                case 0: return null;
                case 1: return $"{singular}: {entry.First()}";
                default: return $"{plural}: {JoinProperties(entry.Select(e => PropertyToString(e)))}";
            }
        }

        protected IEnumerable<string> PropertiesToStrings(IEnumerable<string> properties) => PropertiesToStrings( properties, GetType());
        private IEnumerable<string> PropertiesToStrings(IEnumerable<string> properties, Type type) =>
            from propertyStr in properties
            let property = type.GetProperty(propertyStr)
            let propertyValue = property.GetValue(this)
            let value = GetDisplayNameAndValue(propertyStr, propertyValue)
            where value != null
            select NameToValue(propertyStr, propertyValue);

        protected Tuple<string, string> GetDisplayNameAndValue(string propertyStr, object propertyValue)
        {
            string value;
            switch (propertyValue)
            {
                case null: return null;
                case string str:
                    if (string.IsNullOrWhiteSpace(str)) /* Then */ return null;
                    value = str;
                    break;
                case TimeSpan timeSpan:
                    value = timeSpan.Ticks is 0 ? null 
                          : timeSpan.Hours is 0 ? timeSpan.ToString(@"mm\:ss") 
                                                : timeSpan.ToString(@"hh\:mm\:ss");
                    break;
                case IDictionary dict:
                    if (dict.Count is 0) /* Then */ return null;
                    var formattedDict = dict.Select(p => $"{GetDisplayNameAndValue(propertyStr, p.Key)?.Item2}: {GetDisplayNameAndValue(propertyStr, p.Value)?.Item2}");
                    value = $"{{ {JoinProperties(formattedDict)} }}";
                    break;
                case ICollection collection:
                    var items = collection.Select(i => GetDisplayNameAndValue(propertyStr, i)?.Item2)
                                          .Where(i => i != null)
                                          .ToList();
                    switch (items.Count)
                    {
                        case 0:  return null;
                        case 1:  return GetDisplayNameAndValue(propertyStr.Substring(0, propertyStr.Length - 1), items.First());
                        default: value = $"[ {string.Join(", ", items)} ]"; break;
                    }
                    break;
                case IEnumerable enumerable:
                    return GetDisplayNameAndValue(propertyStr, enumerable.OfType<object>().ToList());
                default: value = propertyValue.ToString(); break;
            }
            return new Tuple<string, string>(propertyStr, value);
        }

        private static readonly Regex NameRegex = new Regex("(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])", 
                                                            RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
        private static string FormatName(string s) => NameRegex.Replace(s, " ");

        protected static string NameToValue(string propertyName, object propertyValue)
            => $"{propertyName}: {PropertyToString(propertyValue)}";

        protected static string JoinProperties(IEnumerable<string> properties) => string.Join("\n", properties);

        protected static string PropertyToString(object property)
        {
            switch (property)
            {
                case null: return string.Empty;
                case string str: return str;
                case TimeSpan timeSpan: return timeSpan.Hours is 0
                        ? timeSpan.ToString(@"mm\:ss")
                        : timeSpan.ToString(@"hh\:mm\:ss");
                case IDictionary<object, object> dict:
                    var formattedDict = dict.Select(p => $"{PropertyToString(p.Key)}: {PropertyToString(p.Value)}");
                    return $"{{\n{JoinProperties(formattedDict)}\n}}";
                case IEnumerable collection:
                    var items = collection.OfType<object>().ToList();
                    switch (items.Count)
                    {
                        case 1:  return PropertyToString(items[0]);
                        default: return "";//$"[ {string.Join(", ", items.Select(PropertyToString))} ]";
                    }

                default: return property.ToString();
            }
        }
    }
}
