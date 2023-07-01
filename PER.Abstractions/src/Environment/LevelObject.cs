using System;

using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

namespace PER.Abstractions.Environment;

[PublicAPI]
public abstract class LevelObject<TLevel, TChunk, TObject>
    where TLevel : Level<TLevel, TChunk, TObject>
    where TChunk : Chunk<TLevel, TChunk, TObject>, new()
    where TObject : LevelObject<TLevel, TChunk, TObject> {
    protected TLevel level {
        get => _level!;
        private set => _level = value;
    }

    protected IRenderer renderer => level.renderer;
    protected IInput input => level.input;
    protected IAudio audio => level.audio;
    protected IResources resources => level.resources;

    protected abstract RenderCharacter character { get; }

    public bool dirty {
        get => _dirty;
        protected set {
            if(_level is null)
                return;
            _dirty = value;
        }
    }

    internal bool positionDirty { get; set; }

    public Guid id {
        get => _id;
        set {
            if(_level is not null)
                throw new InvalidOperationException();
            _id = value;
        }
    }

    public int layer {
        get => _layer;
        set {
            if(_level is not null)
                throw new InvalidOperationException();
            _layer = value;
        }
    }

    internal Vector2Int internalPrevPosition { get; private set; }

    public Vector2Int position {
        get => _position;
        set {
            if(_level is not null && _position != value) {
                dirty = true;
                positionDirty = true;
                // ReSharper disable once SuspiciousTypeConversion.Global
                if(this is IMovable movable)
                    movable.Moved();
            }
            internalPrevPosition = _position;
            _position = value;
        }
    }

    private TLevel? _level;

    private bool _dirty;

    private Guid _id = Guid.NewGuid();
    private int _layer;
    private Vector2Int _position;

    public virtual void Draw() => renderer.DrawCharacter(level.LevelToScreenPosition(position), character);

    internal void SetLevel(Level<TLevel, TChunk, TObject>? level) => _level = level as TLevel;
    internal void ClearDirty() => dirty = false;
}
