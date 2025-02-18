using JetBrains.Annotations;

using NLog;

using PER.Abstractions.Audio;
using PER.Abstractions.Meta;
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

    private static string GetAudioPath(MixerDefinition mixer, AudioResource audio) =>
        $"audio/{mixer.id}/{audio.directory}/{audio.id}.{audio.extension ?? mixer.defaultExtension}";
    public override void Preload() {
        foreach((MixerDefinition mixerDefinition, AudioResource[] audioResources) in sounds)
            foreach(AudioResource audioResource in audioResources)
                AddPath(audioResource.id, GetAudioPath(mixerDefinition, audioResource));
    }

    public override void Load(string id) {
        IAudioMixer master = audio.CreateMixer();

        foreach((MixerDefinition mixerDefinition, AudioResource[] audioResources) in sounds) {
            IAudioMixer mixer = audio.CreateMixer(master);

            foreach((string audioId, _, _, AudioType type) in audioResources)
                switch(type == AudioType.Auto ? mixerDefinition.defaultType : type) {
                    case AudioType.Sfx:
                        AddSound(audioId, mixer);
                        break;
                    case AudioType.Music:
                        AddMusic(audioId, mixer);
                        break;
                }

            audio.TryStoreMixer(mixerDefinition.id, mixer);
        }

        audio.TryStoreMixer(nameof(master), master);
    }

    public override void Unload(string id) => audio.Clear();

    protected void AddSound(string id, IAudioMixer mixer) {
        if(TryGetPath(id, out string? path)) {
            logger.Info("Loading sound {Id}", id);
            audio.TryStorePlayable(id, audio.CreateSound(path, mixer));
        }
        else
            logger.Info("Could not find sound {Id}", id);
    }

    protected void AddMusic(string id, IAudioMixer mixer) {
        if(TryGetPath(id, out string? path)) {
            logger.Info("Loading music {Id}", id);
            audio.TryStorePlayable(id, audio.CreateMusic(path, mixer));
        }
        else
            logger.Info("Could not find music {Id}", id);
    }
}
