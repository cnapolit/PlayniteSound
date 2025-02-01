using PlayniteSounds.Models;
using PlayniteSounds.Models.UI;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Views.Models;
using System;
using System.Collections.Generic;

namespace PlayniteSounds.Services.UI;

public class ModelFactory(ISoundPlayer soundPlayer, IMusicPlayer musicPlayer) : IModelFactory
{
    public ModeSettingsModel CreateModeSettingsModel(ModeSettings settings) => new(this, settings);

    public SoundTypeSettingsModel CreateSoundTypeSettingsModel(SoundTypeSettings settings, bool isDesktop) =>
        new(soundPlayer, settings, isDesktop);

    public IDictionary<UIState, UIStateSettingsModel> CreateUIStateDictionary(ModeSettings settings)
    {
        UIStateSettingsModel CreateUIStateSettingsModel(KeyValuePair<UIState, UIStateSettings> pair,
            bool isDesktop) =>
            new(isDesktop, this, musicPlayer, pair.Value);
        return ConstructDictionary(settings.UIStatesToSettings, CreateUIStateSettingsModel, settings.IsDesktop);
    }

    public IDictionary<PlayniteEvent, SoundTypeSettingsModel> CreatePlayniteEventDictionary(ModeSettings settings)
    {
        SoundTypeSettingsModel CreateSettingsModel(
            KeyValuePair<PlayniteEvent, SoundTypeSettings> pair, bool isDesktop)
            => CreateSoundTypeSettingsModel(pair.Value, isDesktop);
        return ConstructDictionary(
            settings.PlayniteEventToSoundTypesSettings, CreateSettingsModel, settings.IsDesktop);
    }

    private IDictionary<TKey, TOValue> ConstructDictionary<TKey, TIValue, TOValue>(
        IDictionary<TKey, TIValue> settingsDict, 
        Func<KeyValuePair<TKey, TIValue>, bool, TOValue> valueConstructor,
        bool isDesktop)
    {
        var dict = new SortedDictionary<TKey, TOValue>();
        settingsDict.ForEach(p => dict.Add(p.Key, valueConstructor(p, isDesktop)));
        return dict;
    }
}