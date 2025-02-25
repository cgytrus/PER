using JetBrains.Annotations;
using PER.Abstractions;
using PER.Abstractions.Audio;

namespace PER.Audio.Raylib;

[PublicAPI]
public class Audio : IAudio, IDisposable {
    private readonly List<IPlayable> _allPlayables = [];
    private object _playablesLock = new();
    private bool _shouldStop;
    private Thread _thread;

    public IAudioMixer CreateMixer(IAudioMixer? parent = null) => new AudioMixer(parent);
    public IPlayable CreateSound(string filename, IAudioMixer mixer) => AddPlayable(new Sound(filename, mixer));
    public IPlayable CreateMusic(string filename, IAudioMixer mixer) => AddPlayable(new Music(filename, mixer));

    private IPlayable AddPlayable(IPlayable playable) {
        lock (_playablesLock)
            _allPlayables.Add(playable);
        return playable;
    }

    public void UpdateVolumes() {
        lock (_playablesLock) {
            foreach (IPlayable playable in _allPlayables)
                playable.volume = playable.volume;
        }
    }

    public Audio(IAudio.IHandler handler) {
        Raylib_cs.Raylib.InitAudioDevice();
        handler.Audio(this);
        _thread = new Thread(AudioThread);
        _thread.Start();
    }

    public void Dispose() {
        _shouldStop = true;
        _thread.Join();
        foreach (IPlayable? playable in _allPlayables)
            (playable as IDisposable)?.Dispose();
        Raylib_cs.Raylib.CloseAudioDevice();
        GC.SuppressFinalize(this);
    }

    private void AudioThread() {
        while (!_shouldStop) {
            lock (_playablesLock) {
                foreach (IPlayable playable in _allPlayables)
                    (playable as IUpdatable)?.Update(TimeSpan.Zero);
            }
            Thread.Sleep(1000);
        }
    }
}
