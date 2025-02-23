using PER.Abstractions.Audio;
using PER.Abstractions.Meta;

namespace PER.Abstractions.Resources;

[RequiresHead]
public interface IAudioResource<TSelf> : ISingleResource<TSelf, IPlayable?>
    where TSelf : struct, IAudioResource<TSelf> {
    protected IPlayable? playable { get; init; }
    protected static abstract IPlayable Make(string path);

    IPlayable? IResource<TSelf, IPlayable?>.value => playable;
    static TSelf IResource<TSelf>.Load(string path) => new() { playable = TSelf.Make(path) };
    static TSelf IResource<TSelf>.Missing() => new();
}

public interface ISoundResource<TSelf> : IAudioResource<TSelf> where TSelf : struct, ISoundResource<TSelf> {
    public static abstract IAudioMixer mixer { get; }
    static IPlayable IAudioResource<TSelf>.Make(string path) => audio.CreateSound(path, TSelf.mixer);
}

public interface IMusicResource<TSelf> : IAudioResource<TSelf> where TSelf : struct, IMusicResource<TSelf> {
    public static abstract IAudioMixer mixer { get; }
    static IPlayable IAudioResource<TSelf>.Make(string path) => audio.CreateMusic(path, TSelf.mixer);
}
