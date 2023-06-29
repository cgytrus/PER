using JetBrains.Annotations;

using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.UI;
using PER.Util;

using PRR.UI.Resources;

namespace PRR.UI;

public interface IListBoxTemplateFactory<TItem> {
    public abstract class Template {
        public abstract IEnumerable<Element> elements { get; }
        public abstract void UpdateWithItem(int index, TItem item, int width);
        public abstract void MoveTo(Vector2Int origin, int index, Vector2Int size);
        public abstract void Enable();
        public abstract void Disable();
    }

    public Template CreateTemplate();
}

[PublicAPI]
public class ListBox<TItem> : ScrollablePanel {
    public static readonly Type serializedType = typeof(LayoutResource.LayoutResourceListBox<TItem>);

    public IReadOnlyList<TItem> items => _items;

    public TItem this[int i] => items[i];

    private List<TItem> _items = new();
    private List<IListBoxTemplateFactory<TItem>.Template> _elements = new();

    private IListBoxTemplateFactory<TItem> _templateFactory;

    public ListBox(IRenderer renderer, IInput input, IListBoxTemplateFactory<TItem> templateFactory) :
        base(renderer, input) => _templateFactory = templateFactory;

    public void Clear() {
        _items.Clear();
        foreach(IListBoxTemplateFactory<TItem>.Template element in _elements)
            element.Disable();
    }

    public void Add(TItem item) {
        _items.Add(item);
        UpdateElementAt(_items.Count - 1);
    }

    public void Remove(TItem item) => RemoveAt(_items.IndexOf(item));

    public void RemoveAt(int index) {
        if(index < 0 || index >= _items.Count)
            return;
        _items.RemoveAt(index);
        DisableElementAt(index);
    }

    public void Insert(int index, TItem item) {
        _items.Insert(index, item);
        for(int i = index; i < _items.Count; i++)
            UpdateElementAt(i);
    }

    public void Swap(int a, int b) {
        if(a >= _items.Count || b >= _items.Count)
            return;
        (_items[a], _items[b]) = (_items[b], _items[a]);
        SwapElementsAt(a, b);
    }

    public void UpdateItem(TItem item) => UpdateItemAt(_items.IndexOf(item));
    public void UpdateItemAt(int index) {
        if(index < 0 || index >= _items.Count)
            return;
        UpdateElementAt(index);
    }

    private void UpdateElementAt(int index) {
        if(index == _elements.Count) {
            IListBoxTemplateFactory<TItem>.Template template = _templateFactory.CreateTemplate();
            _elements.Add(template);
            elements.AddRange(template.elements);
        }
        else if(index > _elements.Count)
            throw new InvalidOperationException("wtf");
        _elements[index].Enable();
        _elements[index].UpdateWithItem(index, _items[index], size.x);
        _elements[index].MoveTo(position + new Vector2Int(0, scroll), index, size);
    }

    private void DisableElementAt(int index) {
        if(index < 0 || index >= _elements.Count)
            return;
        _elements[_items.Count].Disable();
        for(int i = index; i < _items.Count; i++)
            UpdateElementAt(i);
    }

    private void SwapElementsAt(int a, int b) {
        if(a >= _elements.Count || b >= _elements.Count)
            return;
        Vector2Int offsetPosition = position - new Vector2Int(0, scroll);
        _elements[a].Enable();
        _elements[a].MoveTo(offsetPosition, b, size);
        _elements[b].Enable();
        _elements[b].MoveTo(offsetPosition, a, size);
        (_elements[a], _elements[b]) = (_elements[b], _elements[a]);
    }
}
