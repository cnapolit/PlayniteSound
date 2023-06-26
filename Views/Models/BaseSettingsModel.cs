using PlayniteSounds.Common;
using System.Collections.Generic;

namespace PlayniteSounds.Views.Models
{
    public abstract class BaseSettingsModel : ObservableObject
    {
        protected void UpdateSettings<TSettings>(ref TSettings settings, TSettings value) where TSettings : class
        {
            if (settings is null)
            {
                // Allow main model to pass in value
                settings = value;
            }
            else
            {
                // Copying allows changes to propagate across the plugin due to the settings being singleton
                settings.Copy(value);
            }

            OnPropertyChanged();
        }

        protected int ConvertFromVolume(float volume) => (int)(volume * 100);
        protected float ConvertToVolume(int value) => value / 100f;
    }
}
