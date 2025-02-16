using JetBrains.Annotations;
using PER.Abstractions.Meta;
using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Common.Effects;

[PublicAPI]
public class GlitchEffect : IModifierEffect, IDrawableEffect {
    private bool _draw = true;

    [RequiresHead]
    public void ApplyModifiers(Vector2Int at, ref Vector2Int offset, ref RenderCharacter character) {
        RequireHead();
        offset += new Vector2Int(RandomInt(), 0);
        string mappings = renderer.font.mappings;
        character = new RenderCharacter(
            RandomNonNegativeFloat() <= 0.98f ? character.character : mappings[Random.Shared.Next(0, mappings.Length)],
            RandomizeColor(character.background), RandomizeColor(character.foreground),
            RandomNonNegativeFloat() <= 0.95f ? character.style :
                (RenderStyle)Random.Shared.Next((int)RenderStyle.None, (int)RenderStyle.All));
    }

    [RequiresHead]
    public void Draw(Vector2Int position) {
        RequireHead();
        if(position.x % Random.Shared.Next(3, 10) == 0 || position.y % Random.Shared.Next(3, 10) == 0)
            _draw = RandomNonNegativeFloat() > 0.95f;
        if(!_draw)
            return;
        string mappings = renderer.font.mappings;
        renderer.DrawCharacter(position, new RenderCharacter(
            mappings[Random.Shared.Next(0, mappings.Length)],
            RandomizeColor(Color.transparent), RandomizeColor(Color.white),
            (RenderStyle)Random.Shared.Next((int)RenderStyle.None, (int)RenderStyle.All + 1)));
    }

    private static int RandomInt() => Random.Shared.Next(-1, 2);
    private static float RandomFloat() => Random.Shared.NextSingle(-1f, 1f);
    private static float RandomNonNegativeFloat() => Random.Shared.NextSingle(0f, 1f);
    private static float RandomColorComponent(float current) => current + RandomFloat() * 0.3f;

    private static Color RandomizeColor(Color current) => RandomNonNegativeFloat() <= 0.98f ?
        new Color(RandomColorComponent(current.r), RandomColorComponent(current.g),
            RandomColorComponent(current.b), RandomColorComponent(current.a)) : RandomColor();

    private static Color RandomColor() => Random.Shared.Next(0, 2) == 0 ? new Color(1f, 0f, 0f) :
        Random.Shared.Next(0, 2) == 0 ? new Color(0f, 1f, 0f) : new Color(0f, 0f, 1f);
}
