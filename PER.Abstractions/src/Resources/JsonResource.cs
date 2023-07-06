using JetBrains.Annotations;

namespace PER.Abstractions.Resources;

[PublicAPI]
public abstract class JsonResource<T> : Resource {
    protected void DeserializeAllJson(string id, T deserialized) {
        foreach(string path in GetPaths(id))
            DeserializeJson(path, deserialized);
    }

    protected abstract void DeserializeJson(string path, T deserialized);
}
