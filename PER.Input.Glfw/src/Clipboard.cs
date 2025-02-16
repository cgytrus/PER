using System;
using OpenTK.Windowing.Desktop;
using PER.Abstractions.Input;
using PER.Graphics.OpenGL;

namespace PER.Input.Glfw;

public class Clipboard : Device<Clipboard>, IClipboard {
    private static NativeWindow? window => (renderer as Renderer)?.window;

    public string value {
        get => window?.ClipboardString ?? "";
        set {
            if (window is null)
                return;
            window.ClipboardString = value;
        }
    }

    public override void Setup() { }
    public override void Update(TimeSpan time) { }
    public override void Finish() { }
}
