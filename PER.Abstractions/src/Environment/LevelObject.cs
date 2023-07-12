using System;
using System.Collections.Generic;

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
    protected TLevel level => _level!;

    protected bool inLevel => _level is not null;
    internal bool inLevelInt => inLevel;

    protected IRenderer renderer => level.renderer;
    protected IInput input => level.input;
    protected IAudio audio => level.audio;
    protected IResources resources => level.resources;

    public abstract int layer { get; }
    public abstract RenderCharacter character { get; }
    public virtual IEffect? effect => null;
    public abstract bool blocksLight { get; }

    public bool dirty {
        get => _dirty;
        protected set {
            if(_level is null)
                return;
            _dirty = value;
            level.dirtyObjects.Add((TObject)this);
        }
    }

    public bool lightDirty {
        get => _lightDirty;
        protected set {
            if(_level is null)
                return;
            _lightDirty = value;
            level.dirtyObjects.Add((TObject)this);
        }
    }

    public Guid id { get; protected init; } = Guid.NewGuid();

    internal bool positionDirty { get; private set; }
    internal Vector2Int internalPrevPosition { get; private set; }
    public Vector2Int position {
        get => _position;
        set {
            if(_level is null) {
                internalPrevPosition = _position;
                _position = value;
                return;
            }
            if(_level.updateState is not LevelUpdateState.None and not LevelUpdateState.Tick)
                throw new InvalidOperationException($"{nameof(position)} can only be changed from Tick");
            if(_position == value)
                return;
            dirty = true;
            if(!positionDirty)
                internalPrevPosition = _position;
            positionDirty = true;
            _position = value;
        }
    }

    internal List<ILight?>? blockedLights { get; private set; }
    internal Dictionary<Vector2Int, Color>? contributedLight { get; private set; }

    private TLevel? _level;

    private bool _dirty;
    private bool _lightDirty;

    private Vector2Int _position;

    internal void SetLevel(Level<TLevel, TChunk, TObject>? level) {
        _level = level as TLevel;
        if(level is null)
            return;
        if(blocksLight) {
            if(blockedLights is null)
                blockedLights = new List<ILight?>();
            else
                blockedLights.Clear();
        }
        if(this is not ILight)
            return;
        if(contributedLight is null)
            contributedLight = new Dictionary<Vector2Int, Color>();
        else
            contributedLight.Clear();
    }

    internal void ClearDirty() {
        _dirty = false;
        positionDirty = false;
        _lightDirty = false;
    }
}
