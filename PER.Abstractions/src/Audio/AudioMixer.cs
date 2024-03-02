using JetBrains.Annotations;

namespace PER.Abstractions.Audio;

[PublicAPI]
public class AudioMixer(IAudioMixer? parent = null) : IAudioMixer {
    public IAudioMixer? parent { get; set; } = parent;

    public float volume {
        get => _volume * (parent?.volume ?? 1f);
        set => _volume = value;
    }

    private float _volume = 1f;
}
