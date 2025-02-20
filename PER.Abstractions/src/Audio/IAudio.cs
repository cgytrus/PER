using JetBrains.Annotations;
using PER.Abstractions.Meta;

namespace PER.Abstractions.Audio;

[PublicAPI, RequiresHead]
public interface IAudio : ISetupable {
    public IAudioMixer CreateMixer(IAudioMixer? parent = null);
    public IPlayable CreateSound(string filename, IAudioMixer mixer);
    public IPlayable CreateMusic(string filename, IAudioMixer mixer);

    public void UpdateVolumes();

    public void Clear();
    public void Finish();
}
