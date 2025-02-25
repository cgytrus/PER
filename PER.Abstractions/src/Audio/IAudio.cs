using JetBrains.Annotations;
using PER.Abstractions.Meta;

namespace PER.Abstractions.Audio;

[PublicAPI, RequiresHead]
public interface IAudio {
    public IAudioMixer CreateMixer(IAudioMixer? parent = null);
    public IPlayable CreateSound(string filename, IAudioMixer mixer);
    public IPlayable CreateMusic(string filename, IAudioMixer mixer);

    public void UpdateVolumes();

    public interface IHandler {
        public void Audio(IAudio audio);
    }
}
