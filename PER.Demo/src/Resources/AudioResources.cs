﻿using System;
using System.Collections.Generic;

using PER.Abstractions.Audio;
using PER.Common.Resources;

using PRR.UI;

namespace PER.Demo.Resources;

public class AudioResources : AudioResourcesLoader {
    protected override IAudio audio => Core.engine.audio;

    protected override IReadOnlyDictionary<MixerDefinition, AudioResource[]> sounds { get; } =
        new Dictionary<MixerDefinition, AudioResource[]> {
            { new MixerDefinition("music", AudioType.Music, "ogg"), Array.Empty<AudioResource>() },
            { new MixerDefinition("sfx", AudioType.Sfx, "wav"), new[] {
                new AudioResource(ClickableElement.ClickSoundId),
                new AudioResource(Slider.ValueChangedSoundId),
                new AudioResource(InputField.TypeSoundId),
                new AudioResource(InputField.EraseSoundId),
                new AudioResource(InputField.SubmitSoundId)
            } }
        };
}
