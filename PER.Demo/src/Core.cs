using System;
using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.Screens;
using PER.Graphics.OpenGL;
using PER.Util;
using Keyboard = PER.Graphics.OpenGL.Keyboard;
using Mouse = PER.Graphics.OpenGL.Mouse;

namespace PER.Demo;

public static class Core {
    private static readonly Renderer rend = new("PER Demo Pog", new Vector2Int(80, 60));

    public static IResources resources { get; } = new Common.Resources.Resources();
    public static IRenderer renderer => rend;
    public static IScreens screens { get; } = new Common.Screens.Screens(renderer);
    public static IInput input { get; } = new Input<Keyboard, Mouse>(new KeyboardProvider(rend), new MouseProvider(rend));
    public static IAudio audio { get; } = new Audio.Raylib.Audio();
    public static IGame game { get; } = new Game();

    public static Engine engine { get; } = new(resources, screens, game, renderer, input, audio) {
        updateInterval = TimeSpan.FromSeconds(0d), // no limit
        tickInterval = TimeSpan.FromSeconds(0.08d)
    };

    private static void Main() => engine.Run();
}
