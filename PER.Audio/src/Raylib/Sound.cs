using JetBrains.Annotations;

using PER.Abstractions.Audio;

namespace PER.Audio.Raylib;

[PublicAPI]
public class Sound : IPlayable, IDisposable {
    public IAudioMixer mixer {
        get => _mixer;
        set {
            _mixer = value;
            UpdateVolume();
        }
    }

    public PlaybackStatus status {
        get => Raylib_cs.Raylib.IsSoundPlaying(_sound) ? PlaybackStatus.Playing :
            _status == PlaybackStatus.Playing ? PlaybackStatus.Stopped : _status;
        set {
            _status = value;
            switch(value) {
                case PlaybackStatus.Stopped:
                    Raylib_cs.Raylib.StopSound(_sound);
                    break;
                case PlaybackStatus.Paused:
                    Raylib_cs.Raylib.PauseSound(_sound);
                    break;
                case PlaybackStatus.Playing:
                    if(_status == PlaybackStatus.Paused)
                        Raylib_cs.Raylib.ResumeSound(_sound);
                    else
                        Raylib_cs.Raylib.PlaySound(_sound);
                    break;
            }
        }
    }

    public TimeSpan time {
        get => TimeSpan.Zero;
        set { }
    }

    public float volume {
        get => _volume;
        set {
            _volume = value;
            UpdateVolume();
        }
    }

    public bool looped {
        get => false;
        set { }
    }

    public float pitch {
        get => _pitch;
        set {
            Raylib_cs.Raylib.SetSoundPitch(_sound, pitch);
            _pitch = value;
        }
    }

    public TimeSpan duration => TimeSpan.Zero;

    private readonly Raylib_cs.Sound _sound;
    private IAudioMixer _mixer;
    private PlaybackStatus _status = PlaybackStatus.Stopped;
    private float _volume = 1f;
    private float _pitch = 1f;

    private void UpdateVolume() => Raylib_cs.Raylib.SetSoundVolume(_sound, volume * mixer.volume);

    public Sound(string filename, IAudioMixer mixer) {
        _sound = Raylib_cs.Raylib.LoadSound(filename);
        _mixer = mixer;
        UpdateVolume();
    }

    public void Dispose() {
        Raylib_cs.Raylib.UnloadSound(_sound);
        GC.SuppressFinalize(this);
    }
}
