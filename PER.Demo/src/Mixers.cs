using PER.Abstractions.Audio;
using PER.Abstractions.Meta;
using PRR.UI;

namespace PER.Demo;

#pragma warning disable PER0002
[RequiresHead]
public static class Mixers {
    public static IAudioMixer master { get; set; } = null!;
    public static IAudioMixer music { get; set; } = null!;
    public static IAudioMixer sfx { get; set; } = null!;

    public static void Load() {
        master = audio.CreateMixer();
        music = audio.CreateMixer(master);
        sfx = audio.CreateMixer(master);
        Element.mixer = audio.CreateMixer(sfx);
    }
}
