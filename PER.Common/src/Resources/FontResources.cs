using System.Globalization;
using JetBrains.Annotations;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;
using QoiSharp;

namespace PER.Common.Resources;

[PublicAPI]
public readonly struct FontResources(Image image, FontResources.Mappings mappings) {
    public Font font { get; } = new(image, mappings.mappings, mappings.size);

    public readonly struct Image : IResource<Image, Abstractions.Rendering.Image> {
        private Abstractions.Rendering.Image image { get; init; }

        public Abstractions.Rendering.Image value => image;
        public static string filePath => "graphics/font/font.qoi";

        public static Image Load(string path) {
            QoiImage qoiImage = QoiDecoder.Decode(File.ReadAllBytes(path));
            byte channels = (byte)qoiImage.Channels;
            Abstractions.Rendering.Image image = new(qoiImage.Width, qoiImage.Height);
            for (int i = 0; i < qoiImage.Data.Length; i += channels) {
                int pixelIndex = i / channels;
                int x = pixelIndex % image.width;
                int y = pixelIndex / image.width;
                byte alpha = channels > 3 ? qoiImage.Data[i + 3] : byte.MaxValue;
                image[x, y] = new Color(qoiImage.Data[i], qoiImage.Data[i + 1], qoiImage.Data[i + 2], alpha);
            }
            return new Image { image = image };
        }

        public static Image Merge(Image bottom, Image top) {
            Abstractions.Rendering.Image image = new(Math.Max(bottom.value.width, top.value.width),
                bottom.value.width + top.value.width);
            image.DrawImage(new Vector2Int(0, 0), bottom.value);
            image.DrawImage(new Vector2Int(0, bottom.value.height), top.value);
            return new Image { image = image };
        }

        public static Image Missing() {
            Abstractions.Rendering.Image image = new(2, 2) {
                [0, 0] = new Color(255, 0, 255), [1, 0] = new Color(0,   0, 0),
                [0, 1] = new Color(0,   0, 0),   [1, 1] = new Color(255, 0, 255)
            };
            return new Image { image = image };
        }
    }

    public readonly struct Mappings : IResource<Mappings, Mappings> {
        public Mappings value => this;
        public string mappings { get; private init; }
        public Vector2Int size { get; private init; }

        public static string filePath => "graphics/font/mappings.txt";

        public static Mappings Load(string path) {
            string[] fontMappingsLines = File.ReadAllLines(path);
            string[] fontSizeStr = fontMappingsLines[0].Split(',');
            return new Mappings {
                mappings = fontMappingsLines[1],
                size = new Vector2Int(
                    int.Parse(fontSizeStr[0], CultureInfo.InvariantCulture),
                    int.Parse(fontSizeStr[1], CultureInfo.InvariantCulture)
                )
            };
        }

        public static Mappings Merge(Mappings bottom, Mappings top) =>
            bottom with { mappings = bottom.mappings + top.mappings };

        public static Mappings Missing() => new() { mappings = "\0", size = new Vector2Int(2, 2) };
    }
}
