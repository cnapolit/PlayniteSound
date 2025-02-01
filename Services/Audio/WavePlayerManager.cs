using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Audio;

namespace PlayniteSounds.Services.Audio;

public class WavePlayerManager : IWavePlayerManager, IMMNotificationClient
{
    #region Infrastructure

    private readonly PlayniteSoundsSettings _settings;
    private MMDeviceEnumerator _deviceEnumerator;

    public MixingSampleProvider Mixer { get; }
    public IWavePlayer WavePlayer { get; private set; }

    public WavePlayerManager(PlayniteSoundsSettings settings)
    {
        _settings = settings;
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(settings.AudioSampleRate, settings.AudioChannels);
        Mixer = new MixingSampleProvider(waveFormat) { ReadFully = true };
        Init();
    }

    #endregion

    #region Implementation

    public void Init()
    {
        if (WavePlayer != null)
        {
            WavePlayer.Stop();
            WavePlayer.Dispose();
        }

        switch (_settings.AudioOutput)
        {
            default:                      WavePlayer = new WaveOutEvent();   break;
            case AudioOutput.Wasapi:      WavePlayer = new WasapiOut();      break;
            case AudioOutput.DirectSound: WavePlayer = new DirectSoundOut(); break;
            case AudioOutput.Asio:        WavePlayer = new AsioOut();        break;
        }

        if (_settings.AudioOutput is AudioOutput.Wasapi)
        {
            if (_deviceEnumerator is null)
            {
                _deviceEnumerator = new MMDeviceEnumerator();
                _deviceEnumerator.RegisterEndpointNotificationCallback(this);
            }
        }
        else if (_deviceEnumerator != null)
        {
            _deviceEnumerator.UnregisterEndpointNotificationCallback(this);
            _deviceEnumerator.Dispose();
            _deviceEnumerator = null;
        }
            
        WavePlayer.Init(Mixer);
        WavePlayer.Play();
    }

    public void Dispose()
    {
        _deviceEnumerator?.Dispose();
        WavePlayer?.Dispose();
    }

    #region IMMNotificationClient

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) => Init();

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        // Do Nothing
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        // Do Nothing
    }

    public void OnDeviceRemoved(string deviceId)
    {
        // Do Nothing
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        // Do Nothing
    }

    #endregion

    #endregion
}