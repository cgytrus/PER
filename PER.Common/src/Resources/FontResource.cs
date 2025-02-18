using JetBrains.Annotations;
using PER.Abstractions.Meta;
using PER.Abstractions.Resources;
using PER.Common.Rendering;

namespace PER.Common.Resources;

[PublicAPI]
public class FontResource : HeadResource {
    public const string GlobalId = "graphics/font";

    public Font? font { get; private set; }

    public override void Preload() {
        AddPath("image", "graphics/font/font.qoi");
        AddPath("mappings", "graphics/font/mappings.txt");
    }

    public override void Load(string id) {
        font = new Font(GetPath("image"), GetPath("mappings"));
    }

    public override void Unload(string id) => font = null;
}
