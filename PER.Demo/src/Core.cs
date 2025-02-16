using System;
using PER.Abstractions.Input;
using PER.Abstractions.Meta;
using PER.Graphics.OpenGL;
using PER.Input.Glfw;
using PER.Util;

namespace PER.Demo;

public static class Core {
    [RequiresBody, RequiresHead]
    private static void Main() {
        resources = new Common.Resources.Resources();
        renderer = new Renderer("PER Demo Pog", new Vector2Int(80, 60));
        screens = new Common.Screens.Screens(renderer);
        input = new Input<Keyboard, Mouse, Clipboard>(new Keyboard(), new Mouse(), new Clipboard());
        audio = new Audio.Raylib.Audio();
        game = new Game();
        Engine.updateInterval = TimeSpan.FromSeconds(0d); // no limit
        Engine.tickInterval = TimeSpan.FromSeconds(0.08d);
        Engine.Run();
    }
}
