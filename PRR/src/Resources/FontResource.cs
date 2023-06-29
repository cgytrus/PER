using JetBrains.Annotations;

using PER.Abstractions.Resources;

namespace PRR.Resources;

[PublicAPI]
public class FontResource : Resource {
    public const string GlobalId = "graphics/font";

    public Font? font { get; private set; }

    public override void Preload(IResources resources) {
        AddPath(resources, "image", "graphics/font/font.qoi");
        AddPath(resources, "mappings", "graphics/font/mappings.txt");
    }

    public override void Load(string id) {
        font = new Font(GetPath("image"), GetPath("mappings"));
    }

    public override void Unload(string id) => font = null;
}
