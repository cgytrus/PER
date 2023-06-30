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

    protected bool inLevel { get; private set; }

    public bool dirty {
        get => _dirty;
        protected set {
            if(!inLevel)
                return;
            _dirty = value;
        }
    }

    internal bool positionDirty { get; set; }

    public Guid id {
        get => _id;
        set {
            if(inLevel)
                throw new InvalidOperationException();
            _id = value;
        }
    }

    public int layer {
        get => _layer;
        set {
            if(inLevel)
                throw new InvalidOperationException();
            _layer = value;
        }
    }

    internal Vector2Int internalPrevPosition { get; private set; }
    public Vector2Int position {
        get => _position;
        set {
            if(inLevel && _position != value) {
                dirty = true;
                positionDirty = true;
            }
            internalPrevPosition = _position;
            _position = value;
        }
    }

    private bool _dirty;

    private Guid _id = Guid.NewGuid();
    private int _layer;
    private Vector2Int _position;

    public virtual void Added() => inLevel = true;
    public virtual void Removed() => inLevel = false;

    public virtual void Draw() => renderer.DrawCharacter(level.LevelToScreenPosition(position), character);

    public abstract void Update(TimeSpan time);
    public abstract void Tick(TimeSpan time);

    internal void ClearDirty() => dirty = false;
}

public abstract class LevelObject : LevelObject<Level<LevelObject>> { }
