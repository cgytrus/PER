using System;
using PER.Graphics.OpenGL;
using PER.Util;

namespace PER.Demo;

public static class Core {
    private static readonly Renderer renderer = new("PER Demo Pog", new Vector2Int(80, 60));
    public static Engine engine { get; } =
        new(new Common.Resources.Resources(), new Common.Screens.Screens(renderer), new Game(), renderer,
            new Input(renderer), new Audio.Raylib.Audio()) {
            updateInterval = TimeSpan.FromSeconds(0d), // no limit
            tickInterval = TimeSpan.FromSeconds(0.08d)
        };

    private static void Main() => engine.Run();
}
