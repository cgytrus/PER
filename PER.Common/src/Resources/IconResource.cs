using JetBrains.Annotations;

using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

using QoiSharp;

namespace PER.Common.Resources;

[PublicAPI]
public class IconResource : Resource {
    public const string GlobalId = "graphics/icon";

    public Image? icon { get; private set; }

    public override void Preload() {
        AddPath("icon", "graphics/icon.qoi");
    }

    public override void Load(string id) {
        if(!TryGetPath("icon", out string? icon)) {
            this.icon = null;
            return;
        }
        QoiImage qoiImage = QoiDecoder.Decode(File.ReadAllBytes(icon));
        byte channels = (byte)qoiImage.Channels;
        Image image = new(qoiImage.Width, qoiImage.Height);
        for(int i = 0; i < qoiImage.Data.Length; i += channels) {
            int pixelIndex = i / channels;
            int x = pixelIndex % image.width;
            int y = pixelIndex / image.width;
            byte alpha = channels > 3 ? qoiImage.Data[i + 3] : byte.MaxValue;
            image[x, y] = new Color(qoiImage.Data[i], qoiImage.Data[i + 1], qoiImage.Data[i + 2], alpha);
        }
        this.icon = image;
    }

    public override void Unload(string id) => icon = null;
}
