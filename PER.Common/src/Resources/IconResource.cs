using JetBrains.Annotations;

using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

using QoiSharp;

namespace PER.Common.Resources;

[PublicAPI]
public readonly struct IconResource : IResource<IconResource> {
    public Image? icon { get; private init; }

    public static string filePath => "graphics/icon.qoi";

    public static IconResource Load(string path) {
        QoiImage qoiImage = QoiDecoder.Decode(File.ReadAllBytes(path));
        byte channels = (byte)qoiImage.Channels;
        Image image = new(qoiImage.Width, qoiImage.Height);
        for(int i = 0; i < qoiImage.Data.Length; i += channels) {
            int pixelIndex = i / channels;
            int x = pixelIndex % image.width;
            int y = pixelIndex / image.width;
            byte alpha = channels > 3 ? qoiImage.Data[i + 3] : byte.MaxValue;
            image[x, y] = new Color(qoiImage.Data[i], qoiImage.Data[i + 1], qoiImage.Data[i + 2], alpha);
        }
        return new IconResource { icon = image };
    }

    public static IconResource Merge(IconResource bottom, IconResource top) => top;

    public static IconResource Missing() => new() { icon = null };
}
