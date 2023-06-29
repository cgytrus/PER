using JetBrains.Annotations;

using PER.Abstractions.Resources;

namespace PER.Common.Resources;

[PublicAPI]
public class IconResource : Resource {
    public const string GlobalId = "graphics/icon";

    public string? icon { get; private set; }

    public override void Preload(IResources resources) {
        AddPath(resources, "icon", "graphics/icon.png");
    }

    public override void Load(string id) {
        if(TryGetPath("icon", out string? icon))
            this.icon = icon;
    }

    public override void Unload(string id) => icon = null;
}
