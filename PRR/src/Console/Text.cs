using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using PER.Abstractions.Rendering;
using PER.Util;

namespace PRR.Console;

public class Text {
    private readonly List<IEffect> _globalModEffects;
    private readonly RenderCharacter[,] _display;
    private readonly HashSet<Vector2Int> _displayUsed;
    private readonly Dictionary<Vector2Int, IEffect> _effects;
    private readonly Vector2Int _size;

    public Text(Vector2Int size, List<IEffect> globalModEffects, RenderCharacter[,] display,
        HashSet<Vector2Int> displayUsed, Dictionary<Vector2Int, IEffect> effects) {
        _globalModEffects = globalModEffects;
        _display = display;
        _displayUsed = displayUsed;
        _effects = effects;
        _size = size;
    }

    public void Draw() {
        ConsoleColor origBg = System.Console.BackgroundColor;
        for(int y = 0; y < _size.y; y++) {
            for(int x = 0; x < _size.x; x++) {
                Vector2Int pos = new(x, y);

                if(!_displayUsed.Contains(pos)) {
                    System.Console.BackgroundColor = origBg;
                    System.Console.SetCursorPosition(x, y);
                    System.Console.Write(' ');
                    continue;
                }

                RenderCharacter character = _display[pos.y, pos.x];
                Vector2 modPosition = new(pos.x, pos.y);

                if(_effects.TryGetValue(pos, out IEffect? effect) && effect.hasModifiers)
                    effect.ApplyModifiers(pos, ref modPosition, ref character);

                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach(IEffect globalEffect in _globalModEffects)
                    globalEffect.ApplyModifiers(pos, ref modPosition, ref character);

                Vector2Int position = new((int)MathF.Round(modPosition.X), (int)MathF.Round(modPosition.Y));

                System.Console.BackgroundColor = character.background.a > 0f ?
                    ConsoleConverters.ToConsoleColor(character.background) : origBg;
                System.Console.ForegroundColor = ConsoleConverters.ToConsoleColor(character.foreground);

                // TODO: formatting

                System.Console.SetCursorPosition(position.x, position.y);
                System.Console.Write(character.character);
            }
        }
    }
}
