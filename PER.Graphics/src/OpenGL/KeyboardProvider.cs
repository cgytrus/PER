using PER.Abstractions.Input;

namespace PER.Graphics.OpenGL;

public class KeyboardProvider : DeviceProvider<Keyboard> {
    public KeyboardProvider(Renderer renderer) {
        devices.Add(new Keyboard(renderer));
    }
}
