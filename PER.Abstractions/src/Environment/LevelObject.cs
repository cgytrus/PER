using System;

using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

namespace PER.Abstractions.Environment;

[PublicAPI]
public abstract class LevelObject<TLevel> : IUpdatable, ITickable where TLevel : Level {
    protected static TLevel level => (Level.current as TLevel)!;

    protected static IRenderer renderer => level.renderer;
    protected static IInput input => level.input;
    protected static IAudio audio => level.audio;
    protected static IResources resources => level.resources;

    protected abstract RenderCharacter character { get; }

    internal bool added { get; set; }

    public bool dirty { get; protected set; }

    public Guid id {
        get => _id;
        set {
            if(added)
                throw new InvalidOperationException();
            _id = value;
        }
    }

    public int layer {
        get => _layer;
        set {
            if(_layer != value)
                dirty = true;
            _layer = value;
            if(added && Level.current is not null)
                Level.current.Sort();
        }
    }

    public Vector2Int position {
        get => _position;
        set {
            if(_position != value)
                dirty = true;
            _position = value;
        }
    }

    private Guid _id = Guid.NewGuid();
    private int _layer;
    private Vector2Int _position;

    public virtual void Draw() => renderer.DrawCharacter(level.LevelToScreenPosition(position), character);

    public abstract void Update(TimeSpan time);
    public abstract void Tick(TimeSpan time);

    internal void ClearDirty() => dirty = false;
}

public abstract class LevelObject : LevelObject<Level<LevelObject>> { }
