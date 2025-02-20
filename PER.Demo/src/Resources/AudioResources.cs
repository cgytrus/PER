using System.Collections.Generic;
using PER.Common.Resources;

namespace PER.Demo.Resources;

public class AudioResources : AudioResourcesLoader {
    protected override IReadOnlyDictionary<MixerDefinition, AudioResource[]> sounds { get; } =
        new Dictionary<MixerDefinition, AudioResource[]> {
            { new MixerDefinition("music", AudioType.Music, "ogg"), [] },
            { new MixerDefinition("sfx", AudioType.Sfx, "wav"), [
                new AudioResource("buttonClick"),
                new AudioResource("slider"),
                new AudioResource("inputFieldType"),
                new AudioResource("inputFieldErase"),
                new AudioResource("inputFieldSubmit")
            ] }
        };
}
