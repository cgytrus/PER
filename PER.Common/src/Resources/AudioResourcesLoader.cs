﻿using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

using NLog;

using PER.Abstractions.Audio;
using PER.Abstractions.Resources;

namespace PER.Common.Resources;

[PublicAPI]
public abstract class AudioResourcesLoader : HeadResource {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    protected enum AudioType { Auto, Sfx, Music }
    protected record struct MixerDefinition(string id, AudioType defaultType, string defaultExtension);
    protected record struct AudioResource(string id, string? extension = null, string directory = "",
        AudioType type = AudioType.Auto);

    protected abstract IReadOnlyDictionary<MixerDefinition, AudioResource[]> sounds { get; }

    private readonly Dictionary<string, IAudioMixer> _mixers = new();
    private readonly Dictionary<string, IPlayable> _playables = new();

    private static string GetAudioPath(MixerDefinition mixer, AudioResource audio) =>
        $"audio/{mixer.id}/{audio.directory}/{audio.id}.{audio.extension ?? mixer.defaultExtension}";
    public override void Preload() {
        foreach ((MixerDefinition mixerDefinition, AudioResource[] audioResources) in sounds)
            foreach (AudioResource audioResource in audioResources)
                AddPath(audioResource.id, GetAudioPath(mixerDefinition, audioResource));
    }

    public override void Load(string id) {
        IAudioMixer master = audio.CreateMixer();

        foreach ((MixerDefinition mixerDefinition, AudioResource[] audioResources) in sounds) {
            IAudioMixer mixer = audio.CreateMixer(master);

            foreach ((string audioId, _, _, AudioType type) in audioResources)
                switch (type == AudioType.Auto ? mixerDefinition.defaultType : type) {
                    case AudioType.Sfx:
                        AddSound(audioId, mixer);
                        break;
                    case AudioType.Music:
                        AddMusic(audioId, mixer);
                        break;
                }

            _mixers.TryAdd(mixerDefinition.id, mixer);
        }

        _mixers.TryAdd(nameof(master), master);
    }

    public override void Unload(string id) {
        audio.Clear();
        _playables.Clear();
        _mixers.Clear();
    }

    public bool TryGetMixer(string id, [MaybeNullWhen(false)] out IAudioMixer mixer) =>
        _mixers.TryGetValue(id, out mixer);

    public bool TryGetPlayable(string id, [MaybeNullWhen(false)] out IPlayable playable) =>
        _playables.TryGetValue(id, out playable);

    protected void AddSound(string id, IAudioMixer mixer) {
        if (TryGetPath(id, out string? path)) {
            logger.Info("Loading sound {Id}", id);
            _playables.TryAdd(id, audio.CreateSound(path, mixer));
        }
        else {
            logger.Info("Could not find sound {Id}", id);
        }
    }

    protected void AddMusic(string id, IAudioMixer mixer) {
        if (TryGetPath(id, out string? path)) {
            logger.Info("Loading music {Id}", id);
            _playables.TryAdd(id, audio.CreateMusic(path, mixer));
        }
        else {
            logger.Info("Could not find music {Id}", id);
        }
    }
}
