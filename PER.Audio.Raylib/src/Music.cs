using JetBrains.Annotations;

using PER.Abstractions;
using PER.Abstractions.Audio;

namespace PER.Audio.Raylib;

[PublicAPI]
public class Music : IPlayable, IUpdatable, IDisposable {
    public IAudioMixer mixer {
        get => _mixer;
        set {
            _mixer = value;
            UpdateVolume();
        }
    }

    public PlaybackStatus status {
        get => Raylib_cs.Raylib.IsMusicStreamPlaying(_music) ? PlaybackStatus.Playing :
            _status == PlaybackStatus.Playing ? PlaybackStatus.Stopped : _status;
        set {
            _status = value;
            switch(value) {
                case PlaybackStatus.Stopped:
                    Raylib_cs.Raylib.StopMusicStream(_music);
                    break;
                case PlaybackStatus.Paused:
                    Raylib_cs.Raylib.PauseMusicStream(_music);
                    break;
                case PlaybackStatus.Playing:
                    if(_status == PlaybackStatus.Paused)
                        Raylib_cs.Raylib.ResumeMusicStream(_music);
                    else
                        Raylib_cs.Raylib.PlayMusicStream(_music);
                    break;
            }
        }
    }

    public TimeSpan time {
        get => TimeSpan.FromSeconds(Raylib_cs.Raylib.GetMusicTimePlayed(_music));
        set => Raylib_cs.Raylib.SeekMusicStream(_music, (float)value.TotalSeconds);
    }

    public float volume {
        get => _volume;
        set {
            _volume = value;
            UpdateVolume();
        }
    }

    public bool looped {
        get => _music.Looping;
        set => _music.Looping = value;
    }

    public float pitch {
        get => _pitch;
        set {
            Raylib_cs.Raylib.SetMusicPitch(_music, value);
            _pitch = value;
        }
    }

    public TimeSpan duration => TimeSpan.FromSeconds(Raylib_cs.Raylib.GetMusicTimeLength(_music));

    private Raylib_cs.Music _music;
    private IAudioMixer _mixer;
    private PlaybackStatus _status = PlaybackStatus.Stopped;
    private float _volume = 1f;
    private float _pitch = 1f;

    private void UpdateVolume() => Raylib_cs.Raylib.SetMusicVolume(_music, volume * mixer.volume);

    private Music(Raylib_cs.Music music, IAudioMixer mixer) {
        _music = music;
        _mixer = mixer;
        UpdateVolume();
    }

    public Music(string filename, IAudioMixer mixer) : this(Raylib_cs.Raylib.LoadMusicStream(filename), mixer) { }

    public void Dispose() {
        Raylib_cs.Raylib.UnloadMusicStream(_music);
        GC.SuppressFinalize(this);
    }

    public void Update(TimeSpan time) => Raylib_cs.Raylib.UpdateMusicStream(_music);
}
