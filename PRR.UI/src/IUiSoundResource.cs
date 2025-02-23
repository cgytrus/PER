using PER.Abstractions.Audio;
using PER.Abstractions.Resources;

namespace PRR.UI;

public interface IUiSoundResource<TSelf> : ISoundResource<TSelf> where TSelf : struct, IUiSoundResource<TSelf> {
    static IAudioMixer ISoundResource<TSelf>.mixer => Element.mixer;
}
