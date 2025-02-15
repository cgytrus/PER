using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using PER.Abstractions;
using PER.Abstractions.Audio;

namespace PER.Audio.Raylib;

[PublicAPI]
public class Audio : IAudio {
    private readonly Dictionary<string, IAudioMixer> _storedMixers = new();

    private readonly List<IPlayable> _allPlayables = new();
    private readonly Dictionary<string, IPlayable> _storedPlayables = new();

    private object _playablesLock = new();
    private bool _shouldStop;
    private Thread? _thread;

    public IAudioMixer CreateMixer(IAudioMixer? parent = null) => new AudioMixer(parent);
    public bool TryStoreMixer(string id, IAudioMixer mixer) => _storedMixers.TryAdd(id, mixer);
    public bool TryGetMixer(string id, [MaybeNullWhen(false)] out IAudioMixer mixer) =>
        _storedMixers.TryGetValue(id, out mixer);

    public IPlayable CreateSound(string filename, IAudioMixer mixer) => AddPlayable(new Sound(filename, mixer));
    public IPlayable CreateMusic(string filename, IAudioMixer mixer) => AddPlayable(new Music(filename, mixer));

    private IPlayable AddPlayable(IPlayable playable) {
        lock(_playablesLock)
            _allPlayables.Add(playable);
        return playable;
    }

    public bool TryStorePlayable(string id, IPlayable playable) {
        lock(_playablesLock)
            return _storedPlayables.TryAdd(id, playable);
    }
    public bool TryGetPlayable(string id, [MaybeNullWhen(false)] out IPlayable playable) {
        lock(_playablesLock)
            return _storedPlayables.TryGetValue(id, out playable);
    }

    public void UpdateVolumes() {
        lock(_playablesLock)
            foreach(IPlayable playable in _allPlayables)
                playable.volume = playable.volume;
    }

    public void Clear() {
        lock(_playablesLock) {
            foreach(IPlayable? playable in _allPlayables)
                if(playable is IDisposable disposable)
                    disposable.Dispose();
            _allPlayables.Clear();
            _storedPlayables.Clear();
        }
        _storedMixers.Clear();
    }

    public void Setup() {
        Raylib_cs.Raylib.InitAudioDevice();
        _thread = new Thread(AudioThread);
        _thread.Start();
    }

    public void Finish() {
        _shouldStop = true;
        Clear();
        Raylib_cs.Raylib.CloseAudioDevice();
        _thread!.Join();
    }

    private void AudioThread() {
        while(!_shouldStop) {
            lock(_playablesLock)
                foreach(IPlayable playable in _allPlayables)
                    if(playable is IUpdatable updatable)
                        updatable.Update(TimeSpan.Zero);
            Thread.Sleep(1000);
        }
    }
}
