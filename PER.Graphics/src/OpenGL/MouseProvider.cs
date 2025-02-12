using PER.Abstractions.Input;

namespace PER.Graphics.OpenGL;

public class MouseProvider : DeviceProvider<Mouse> {
    public MouseProvider(Renderer renderer) {
        devices.Add(new Mouse(renderer));
    }
}
