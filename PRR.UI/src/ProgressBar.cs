using JetBrains.Annotations;

using PER.Abstractions.Rendering;
using PER.Abstractions.UI;
using PER.Util;

using PRR.UI.Resources;

namespace PRR.UI;

[PublicAPI]
public class ProgressBar : Element {
    public static readonly Type serializedType = typeof(LayoutResource.LayoutResourceProgressBar);

    private struct AnimatedCharacter {
        private float speed { get; set; }
        private TimeSpan startTime { get; set; }
        private Color colorStart { get; set; }
        private Color colorEnd { get; set; }

        private const float MinSpeed = 3f;
        private const float MaxSpeed = 5f;

        public void Start(TimeSpan startTime, Color color) {
            if(colorEnd == color) return;
            speed = Random.Shared.NextSingle(MinSpeed, MaxSpeed);
            this.startTime = startTime;
            colorStart = colorEnd;
            colorEnd = color;
        }

        public Color Get(TimeSpan time) {
            float t = (float)(time - startTime).TotalSeconds * speed;
            return Color.LerpColors(colorStart, colorEnd, t);
        }
    }

    public override Vector2Int size {
        get => base.size;
        set {
            base.size = value;
            _anim = new AnimatedCharacter[value.x, value.y];
            _resized = true;
        }
    }

    public float value { get; set; }

    public Color lowColor { get; set; } = Color.black;
    public Color highColor { get; set; } = Color.white;

    private bool _resized;
    private AnimatedCharacter[,] _anim = new AnimatedCharacter[0, 0];
    private float _prevValue;

    public ProgressBar(IRenderer renderer) : base(renderer) { }

    public static ProgressBar Clone(ProgressBar template) => new(template.renderer) {
        enabled = template.enabled,
        position = template.position,
        size = template.size,
        effect = template.effect,
        value = template.value,
        lowColor = template.lowColor,
        highColor = template.highColor
    };

    public override Element Clone() => Clone(this);

    private void Animate(TimeSpan time, float from, float to, Color lowColor, Color highColor) {
        int fromX = (int)MathF.Floor(size.x * MathF.Min(MathF.Max(from, 0f), 1f));
        int toX = (int)MathF.Floor(size.x * MathF.Min(MathF.Max(to, 0f), 1f));
        if(fromX == toX) return;
        Color color = fromX < toX ? highColor : lowColor;
        int min = Math.Min(fromX, toX);
        int max = Math.Max(fromX, toX);
        for(int x = min; x < max; x++)
            for(int y = 0; y < size.y; y++)
                _anim[x, y].Start(time, color);
    }

    public override void Update(TimeSpan time) {
        if(!enabled) return;

        if(value != _prevValue) Animate(time, _prevValue, value, lowColor, highColor);
        else if(_resized) {
            Animate(time, value, value, lowColor, highColor);
            _resized = false;
        }
        _prevValue = value;

        for(int x = 0; x < size.x; x++)
            for(int y = 0; y < size.y; y++)
                renderer.DrawCharacter(new Vector2Int(position.x + x, position.y + y),
                    new RenderCharacter('\0', _anim[x, y].Get(time), Color.transparent), RenderOptions.Default,
                    effect);
    }

    public override void UpdateColors(Dictionary<string, Color> colors, string layoutName, string id, string? special) {
        if(TryGetColor(colors, "progressBar", layoutName, id, "low", special, out Color color))
            lowColor = color;
        if(TryGetColor(colors, "progressBar", layoutName, id, "high", special, out color))
            highColor = color;
    }
}
