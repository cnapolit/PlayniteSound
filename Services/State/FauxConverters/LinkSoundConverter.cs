using Playnite.SDK;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Audio.Sound;
using PlayniteSounds.Services.Audio;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PlayniteSounds.Services.State.FauxConverters
{
    public class LinkSoundConverter : ILinkSoundConverter
    {
        private readonly ILogger _logger;
        private readonly ISoundPlayer _soundPlayer;
        protected readonly IPlayniteEventHandler _playniteEventHandler;

        public LinkSoundConverter(ILogger logger, ISoundPlayer soundPlayer, IPlayniteEventHandler playniteEventHandler)
        {
            _logger = logger;
            _soundPlayer = soundPlayer;
            _playniteEventHandler = playniteEventHandler;
        }

        public object Convert(object value, Type _, object parameter, CultureInfo __)
        {
            if (parameter is string parameterStr)
            {
                var startingIndex = 0;
                var firstColonIndex = 0;
                var secondColonIndex = 0;
                for (var i = 0; i < parameterStr.Length; i++)
                {
                    var firstColon = parameterStr[i];
                    firstColonIndex = i;
                    if (firstColon is ':') /* Then */ while (++i < parameterStr.Length)
                    {
                        var secondColon = parameterStr[i];
                        secondColonIndex = i;
                        if (secondColon is ':')
                        {
                            while (++i < parameterStr.Length)
                            {
                                var underscore = parameterStr[i];
                                var underscoreIndex = i;
                                if (underscore is '_')
                                {
                                    CreateLink(
                                        value as UIElement,
                                        parameterStr,
                                        startingIndex,
                                        firstColonIndex,
                                        secondColonIndex,
                                        underscoreIndex);
                                    startingIndex = underscoreIndex + 1;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }

                CreateLink(
                    value as UIElement,
                    parameterStr,
                    startingIndex,
                    firstColonIndex,
                    secondColonIndex,
                    parameterStr.Length);
            }

            return string.Empty;
        }

        private string Slice(string str, int startIndex, int endIndex) 
            => str.Substring(startIndex, endIndex - startIndex);

        private void CreateLink(
            UIElement element,
            string parameters,
            int startingIndex,
            int firstSeparatorIndex,
            int secondSeparatorIndex,
            int endingIndex)
        {
            var uiStateStr   = Slice(parameters, startingIndex, firstSeparatorIndex);
            var linkMethod   = Slice(parameters, firstSeparatorIndex + 1, secondSeparatorIndex);
            var soundTypeStr = Slice(parameters, secondSeparatorIndex + 1, endingIndex);

            /* When */ if (Enum.TryParse<UIState>(uiStateStr, true, out var uiState))
            /* Then */ if (Enum.TryParse<SoundType>(soundTypeStr, true, out var soundType))
            /* Then */ switch (linkMethod)
            {
                case "LostFocus":
                    HandleRoutedEvent(element, UIElement.LostFocusEvent, uiState, soundType); return;
                case "GotFocus":
                    HandleRoutedEvent(element, UIElement.GotFocusEvent, uiState, soundType); return;
                case "Click":
                    HandleRoutedEvent(element, ButtonBase.ClickEvent, uiState, soundType); return;
                case "Loaded":
                    HandleRoutedEvent(element, FrameworkElement.LoadedEvent, uiState, soundType); return;
                case "UnLoaded":
                    HandleRoutedEvent(element, FrameworkElement.UnloadedEvent, uiState, soundType); return;
                case "IsVisibilityChanged":
                    element.IsVisibleChanged += 
                        (_, arg) => { if (arg.NewValue is true) Trigger(uiState, soundType); }; return;
                case "Initialized":
                    (element as FrameworkElement).Initialized += (_, __) => Trigger(uiState, soundType); return;
                case "TargetUpdated":
                    (element as FrameworkElement).TargetUpdated += (_, __) => Trigger(uiState, soundType); return;
                case "SourceUpdated":
                    (element as FrameworkElement).SourceUpdated += (_, __) => Trigger(uiState, soundType); return;
                case "SelectionChanged":
                    (element as ComboBox).SelectionChanged += (_, __) => Trigger(uiState, soundType); return;
                case "DragCompleted":
                    (element as Thumb).DragCompleted += (_, __) => Trigger(uiState, soundType); return;
                    default: return;
            }
            else /* Then */ switch (linkMethod)
            {
                case "LostFocus":
                    HandleRoutedEvent(element, UIElement.LostFocusEvent, uiState); return;
                case "GotFocus":
                    HandleRoutedEvent(element, UIElement.GotFocusEvent, uiState); return;
                case "Click":
                    HandleRoutedEvent(element, ButtonBase.ClickEvent, uiState); return;
                case "Loaded":
                    HandleRoutedEvent(element, FrameworkElement.LoadedEvent, uiState); return;
                case "UnLoaded":
                    HandleRoutedEvent(element, FrameworkElement.UnloadedEvent, uiState); return;
                case "IsVisibilityChanged":
                    element.IsVisibleChanged += (_, arg) => 
                    { if (arg.NewValue is true) _playniteEventHandler.TriggerUIStateChanged(uiState); }; return;
                case "Initialized":
                    (element as FrameworkElement).Initialized += 
                        (_, __) => _playniteEventHandler.TriggerUIStateChanged(uiState); return;
                case "TargetUpdated":
                    (element as FrameworkElement).TargetUpdated += 
                        (_, __) => _playniteEventHandler.TriggerUIStateChanged(uiState); return;
                case "SourceUpdated":
                    (element as FrameworkElement).SourceUpdated +=
                        (_, __) => _playniteEventHandler.TriggerUIStateChanged(uiState); return;
                case "SelectionChanged":
                    (element as ComboBox).SelectionChanged +=
                        (_, __) => _playniteEventHandler.TriggerUIStateChanged(uiState); return;
                case "DragCompleted":
                    (element as Thumb).DragCompleted +=
                        (_, __) => _playniteEventHandler.TriggerUIStateChanged(uiState); return;
                case "Selected":
                    (element as ComboBoxItem).Selected +=
                        (_, __) => _playniteEventHandler.TriggerUIStateChanged(uiState); return;
                default: return;
            }
        }

        private void HandleRoutedEvent(
            UIElement element, RoutedEvent routedEvent, UIState uiState, SoundType soundType)
            => element.AddHandler(routedEvent, new RoutedEventHandler((_, __) => Trigger(uiState, soundType)));

        private void HandleRoutedEvent(UIElement element, RoutedEvent routedEvent, UIState uiState)
            => element.AddHandler(
                routedEvent, new RoutedEventHandler((_, __) => _playniteEventHandler.TriggerUIStateChanged(uiState)));

        private void Trigger(UIState uiState, SoundType soundType)
        {
            _playniteEventHandler.TriggerUIStateChanged(uiState);
            _soundPlayer.Trigger(soundType);
        }

        protected void LogWarning(string messagePrefix, Type type)
            => _logger.Warn($"{messagePrefix} while attempting to link '{type.Name}' for LinkSoundConverter");

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => string.Empty;
    }
}
