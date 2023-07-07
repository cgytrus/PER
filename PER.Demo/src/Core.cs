using System;

using PER.Audio.Sfml;
using PER.Common.Resources;
using PER.Common.Screens;

using PRR.Ogl;

namespace PER.Demo;

public static class Core {
    private static readonly Renderer renderer = new();
    public static Engine engine { get; } =
        new(new ResourcesManager(), new ScreenManager(renderer), new Game(), renderer, new InputManager(renderer), new AudioManager()) {
            updateInterval = TimeSpan.FromSeconds(0d), // no limit
            tickInterval = TimeSpan.FromSeconds(0.08d)
        };

    private static void Main() => engine.Reload();
}
