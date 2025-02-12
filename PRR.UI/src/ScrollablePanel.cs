using JetBrains.Annotations;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Util;

using PRR.UI.Resources;

namespace PRR.UI;

[PublicAPI]
public class ScrollablePanel(IRenderer renderer, IInput input) : Element(renderer) {
    public static readonly Type serializedType = typeof(LayoutResourceScrollablePanel);

    public IInput input { get; set; } = input;

    public List<Element> elements { get; } = [];

    public int scroll {
        get => _scroll;
        set {
            int delta = value - _scroll;
            _scroll = value;
            foreach(Element element in elements)
                element.position += new Vector2Int(0, delta);
        }
    }

    private int _scroll;

    private InputReq<(float, Mouse.Positions)> _scrollReq;

    public override Element Clone() => throw new NotImplementedException();

    public override void Input() {
        if (!enabled || elements.Count == 0)
            return;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (int i = 0; i < elements.Count; i++) {
            Element element = elements[i];
            if (element.position.y < bounds.min.y || element.position.y > bounds.max.y)
                continue;
            elements[i].Input();
        }
        _scrollReq = input.Get<Mouse>().GetScroll(bounds);
    }

    public override void Update(TimeSpan time) {
        if (!enabled || elements.Count == 0)
            return;

        int delta = (int)_scrollReq.Read().Item1;
        if (delta != 0) {
            int lowestY = int.MaxValue;
            int highestY = int.MinValue;
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (Element element in elements) {
                if (!element.enabled)
                    continue;
                if (element.bounds.min.y < lowestY)
                    lowestY = element.bounds.min.y;
                if (element.bounds.max.y > highestY)
                    highestY = element.bounds.max.y;
            }

            if (delta < 0 && highestY + delta < bounds.max.y ||
                delta > 0 && lowestY + delta > bounds.min.y)
                return;

            scroll += delta;
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for (int i = 0; i < elements.Count; i++) {
            Element element = elements[i];
            if (element.position.y < bounds.min.y || element.position.y > bounds.max.y)
                continue;
            elements[i].Update(time);
        }
    }

    public override void UpdateColors(Dictionary<string, Color> colors, List<string> layoutNames, string id,
        string? special) { }

    protected record LayoutResourceScrollablePanel(bool? enabled, Vector2Int position, Vector2Int size) :
        LayoutResource.LayoutResourceElement(enabled, position, size) {
        public override Element GetElement(LayoutResource resource, IRenderer renderer,
            IInput input, IAudio audio, Dictionary<string, Color> colors, List<string> layoutNames, string id) {
            ScrollablePanel element = new(renderer, input) {
                position = position,
                size = size
            };
            if (enabled.HasValue) element.enabled = enabled.Value;
            element.UpdateColors(colors, layoutNames, id, null);
            return element;
        }
    }
}
