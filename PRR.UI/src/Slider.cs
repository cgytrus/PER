using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Util;

using PRR.UI.Resources;

namespace PRR.UI;

[PublicAPI]
public class Slider(IRenderer renderer, IInput input, IAudio? audio = null) : ClickableElement(renderer, input, audio) {
    public static readonly Type serializedType = typeof(LayoutResourceSlider);

    protected override string type => "slider";

    public const string ValueChangedSoundId = "slider";

    public override Vector2Int size {
        get => base.size;
        set => base.size = value.y != 1 ? new Vector2Int(value.x, 1) : value;
    }

    public int width {
        get => size.x;
        set => size = new Vector2Int(value, 1);
    }

    public float value {
        get => _value;
        set {
            if(_value == value) return;
            _value = value;
            float tempValue = (value - minValue) / (maxValue - minValue);
            _relativeValue = (int)(tempValue * (width - 1));
            onValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public float minValue { get; set; }
    public float maxValue { get; set; }

    public IPlayable? valueChangedSound { get; set; }
    public event EventHandler? onValueChanged;

    protected override bool hotkeyPressed => false;

    private int _relativeValue;
    private float _value = float.NaN; // make first value set always work

    public override Element Clone() => throw new NotImplementedException();

    protected override void UpdateState(TimeSpan time) {
        base.UpdateState(time);
        if(currentState == State.Clicked)
            UpdateValue();
    }

    private void UpdateValue() {
        int prevRelativeValue = _relativeValue;
        _relativeValue = Math.Clamp(input.mousePosition.x - position.x, 0, width - 1);
        if(prevRelativeValue == _relativeValue) return;
        float tempValue = (float)_relativeValue / (width - 1);
        tempValue = minValue + tempValue * (maxValue - minValue);
        value = Math.Clamp(tempValue, minValue, maxValue);

        PlaySound(audio, valueChangedSound, ValueChangedSoundId);
    }

    protected override void DrawCharacter(int x, int y, Color backgroundColor, Color foregroundColor) {
        Vector2Int position = new(this.position.x + x, this.position.y + y);
        char character = x < _relativeValue ? '─' : x == _relativeValue ? '█' : '-';
        renderer.DrawCharacter(position, new RenderCharacter(character, backgroundColor, foregroundColor), effect);
    }

    protected override void CustomUpdate(TimeSpan time) { }

    private record LayoutResourceSlider(bool? enabled, Vector2Int position, Vector2Int size, int? width, float? value,
        float? minValue, float? maxValue, bool? active) :
        LayoutResource.LayoutResourceElement(enabled, position, size) {
        public override Element GetElement(LayoutResource resource, IRenderer renderer,
            IInput input, IAudio audio, Dictionary<string, Color> colors, List<string> layoutNames, string id) {
            Slider element = new(renderer, input, audio) {
                position = position,
                size = size
            };
            if(enabled.HasValue) element.enabled = enabled.Value;
            if(width.HasValue) element.width = width.Value;
            if(minValue.HasValue) element.minValue = minValue.Value;
            if(maxValue.HasValue) element.maxValue = maxValue.Value;
            if(value.HasValue) element.value = value.Value;
            if(active.HasValue) element.active = active.Value;
            element.UpdateColors(colors, layoutNames, id, null);
            return element;
        }
    }
}
