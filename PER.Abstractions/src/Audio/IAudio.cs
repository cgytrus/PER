﻿using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

namespace PER.Abstractions.Audio;

[PublicAPI]
public interface IAudio {
    public IAudioMixer CreateMixer(IAudioMixer? parent = null);

    public bool TryStoreMixer(string id, IAudioMixer mixer);
    public bool TryGetMixer(string id, [MaybeNullWhen(false)] out IAudioMixer mixer);

    public IPlayable CreateSound(string filename, IAudioMixer mixer);
    public IPlayable CreateMusic(string filename, IAudioMixer mixer);

    public bool TryStorePlayable(string id, IPlayable playable);
    public bool TryGetPlayable(string id, [MaybeNullWhen(false)] out IPlayable playable);

    public void UpdateVolumes();

    public void Clear();
    public void Finish();
}
