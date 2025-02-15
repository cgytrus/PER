using System;
using PER.Abstractions.Input;

namespace PER.Graphics.OpenGL;

public class Clipboard(Renderer renderer) : Device<Clipboard>, IClipboard {
    public string value {
        get => renderer.window?.ClipboardString ?? "";
        set {
            if(renderer.window is null)
                return;
            renderer.window.ClipboardString = value;
        }
    }

    public override void Setup() { }
    public override void Update(TimeSpan time) { }
    public override void Finish() { }
}
