﻿using System.IO;
using System.Text.Json;

using PER.Abstractions.Audio;
using PER.Abstractions.Meta;
using PER.Demo.Resources;

namespace PER.Demo;

public class Settings {
    public string[] packs { get; set; } = ["Default"];
    public float volume { get; set; } = 0.2f;

    public static Settings Load(string path) {
        if(!File.Exists(path))
            return new Settings();
        FileStream file = File.OpenRead(path);
        Settings settings = JsonSerializer.Deserialize<Settings>(file) ?? new Settings();
        file.Close();
        return settings;
    }

    public void Save(string path) {
        FileStream file = File.Open(path, FileMode.Create);
        JsonSerializer.Serialize(file, this);
        file.Close();
    }

    [RequiresHead]
    public void Apply() {
        if (!resources.TryGetResource("audio", out AudioResources? audioRes))
            return;
        if (!audioRes.TryGetMixer("master", out IAudioMixer? mixer))
            return;
        mixer.volume = volume;
        audio.UpdateVolumes();
    }
}
