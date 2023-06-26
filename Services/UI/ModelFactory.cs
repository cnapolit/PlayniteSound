using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Views.Models;
using System;
using System.Collections.Generic;

namespace PlayniteSounds.Services.UI
{
    public class ModelFactory : IModelFactory
    {
        private readonly ISoundPlayer _soundPlayer;
        private readonly IMusicPlayer _musicPlayer;

        public ModelFactory(ISoundPlayer soundPlayer, IMusicPlayer musicPlayer)
        {
            _soundPlayer = soundPlayer;
            _musicPlayer = musicPlayer;
        }

        public ModeSettingsModel CreateModeSettingsModel(ModeSettings settings)
            => new ModeSettingsModel(this, settings);

        public IDictionary<UIState, UIStateSettingsModel> CreateUIStateDictionary(ModeSettings settings)
        {
            UIStateSettingsModel CreateUIStateSettingsModel(KeyValuePair<UIState, UIStateSettings> pair)
            {
                var soundTypeDictionary = ConstructSoundTypeDictionary(pair.Value, settings.IsDesktop);
                return new UIStateSettingsModel(_musicPlayer, soundTypeDictionary, pair.Value);
            }
            return ConstructDictionary(settings.UIStatesToSettings, CreateUIStateSettingsModel);
        }

        private IDictionary<SoundType, SoundTypeSettingsModel> ConstructSoundTypeDictionary(
            UIStateSettings settings, bool isDesktop)
            => ConstructDictionary(
                settings.SoundTypesToSettings,
                p => new SoundTypeSettingsModel(p.Key, _soundPlayer, p.Value, isDesktop));

        private IDictionary<TKey, TOValue> ConstructDictionary<TKey, TIValue, TOValue>(
            IDictionary<TKey, TIValue> settingsDict, Func<KeyValuePair<TKey, TIValue>, TOValue> valueConstructor)
        {
            var dict = new SortedDictionary<TKey, TOValue>();
            settingsDict.ForEach(p => dict.Add(p.Key, valueConstructor(p)));
            return dict;
        }
    }
}
