﻿using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Demo.Environment;

public class FloorObject : LevelObject {
    protected override RenderCharacter character { get; } =
        new('.', Color.transparent, new Color(0.1f, 0.1f, 0.1f, 1f));
}
