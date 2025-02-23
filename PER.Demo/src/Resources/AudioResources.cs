using PER.Abstractions.Audio;
using PER.Abstractions.Resources;

namespace PER.Demo.Resources;

public static class AudioResources {
    public readonly struct ButtonClick : ISoundResource<ButtonClick> {
        public IPlayable? value { get; init; }
        public static string filePath => "audio/ui/buttonClick.wav";
        public static IAudioMixer mixer => Mixers.sfx;
    }
    public readonly struct Slider : ISoundResource<Slider> {
        public IPlayable? value { get; init; }
        public static string filePath => "audio/ui/slider.wav";
        public static IAudioMixer mixer => Mixers.sfx;
    }
    public readonly struct InputFieldType : ISoundResource<InputFieldType> {
        public IPlayable? value { get; init; }
        public static string filePath => "audio/ui/inputFieldType.wav";
        public static IAudioMixer mixer => Mixers.sfx;
    }
    public readonly struct InputFieldErase : ISoundResource<InputFieldErase> {
        public IPlayable? value { get; init; }
        public static string filePath => "audio/ui/inputFieldErase.wav";
        public static IAudioMixer mixer => Mixers.sfx;
    }
    public readonly struct InputFieldSubmit : ISoundResource<InputFieldSubmit> {
        public IPlayable? value { get; init; }
        public static string filePath => "audio/ui/inputFieldSubmit.wav";
        public static IAudioMixer mixer => Mixers.sfx;
    }
}
