using Playnite.SDK;
using PlayniteSounds.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PlayniteSounds.Services.State.FauxConverters
{
    public abstract class BaseUIStateFauxConverter<T> : IValueConverter
    {
        private   readonly ILogger               _logger;
        protected readonly IPlayniteEventHandler _playniteEventHandler;

        public BaseUIStateFauxConverter(ILogger logger, IPlayniteEventHandler playniteEventHandler)
        {
            _logger = logger;
            _playniteEventHandler = playniteEventHandler;
        }

        public object Convert(object value, Type type, object parameter, CultureInfo _)
        {
            if (Enum.TryParse<UIState>(parameter as string, out var uiState))
            /* Then */ if (value is T typedValue) /* Then */ Link(typedValue, uiState);
                       else                       /* Then */ LogWarning($"Encountered type mismatch for type '{type}'");
            else /* Then */ LogWarning($"Unable to parse parameter '{parameter}'as enum UIState");
            return string.Empty;
        }

        protected abstract void Link(T value, UIState state);

        protected void LogWarning(string messagePrefix) 
            => _logger.Warn($"{messagePrefix} while attempting to link '{typeof(T)}' for IValueConverter '{GetType()}'");

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => string.Empty;
    }
}
