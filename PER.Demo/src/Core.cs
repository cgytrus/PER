using System;
using PER.Abstractions.Input;
using PER.Graphics.OpenGL;
using PER.Util;
using Keyboard = PER.Graphics.OpenGL.Keyboard;
using Mouse = PER.Graphics.OpenGL.Mouse;

namespace PER.Demo;

public static class Core {
    private static readonly Renderer renderer = new("PER Demo Pog", new Vector2Int(80, 60));
    public static Engine engine { get; } =
        new(new Common.Resources.Resources(), new Common.Screens.Screens(renderer), new Game(), renderer,
            new Input<Keyboard, Mouse>(new KeyboardProvider(renderer), new MouseProvider(renderer)),
            new Audio.Raylib.Audio()) {
            updateInterval = TimeSpan.FromSeconds(0d), // no limit
            tickInterval = TimeSpan.FromSeconds(0.08d)
        };

    private static void Main() => engine.Run();
}
