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

    public Vector2Int position {
        get => _position;
        set {
            if(Level.current is not null)
                Level.current.ObjectMoved(this, _position, value);
            _position = value;
        }
    }

    private Vector2Int _position;

    public virtual void Draw() {
        if(Level.current is null)
            return;
        Vector2Int drawPos = position - Level.current.cameraPosition +
            new Vector2Int(renderer.width / 2, renderer.height / 2);
        renderer.DrawCharacter(drawPos, character);
    }

    public abstract void Update(TimeSpan time);
    public abstract void Tick(TimeSpan time);
}
