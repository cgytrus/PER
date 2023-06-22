using System;

using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

namespace PER.Abstractions.Environment;

[PublicAPI]
public abstract class LevelObject : IUpdatable, ITickable {
    protected static Level level => Level.current!;

    protected static IRenderer renderer => level.renderer;
    protected static IInput input => level.input;
    protected static IAudio audio => level.audio;
    protected static IResources resources => level.resources;

    protected abstract RenderCharacter character { get; }

    public Vector2Int position { get; set; }
    public Vector2Int screenPosition => position - level.cameraPosition;
    public Vector2Int drawPosition => screenPosition + new Vector2Int(renderer.width / 2, renderer.height / 2);

    public virtual void Draw() => renderer.DrawCharacter(drawPosition, character);

    public abstract void Update(TimeSpan time);
    public abstract void Tick(TimeSpan time);
}
