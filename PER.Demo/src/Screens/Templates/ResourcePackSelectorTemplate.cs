using System.Collections.Generic;
using PER.Abstractions.Meta;
using PER.Abstractions.Resources;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace PER.Demo.Screens.Templates;

public class ResourcePackSelectorTemplate(
    GameScreen screen,
    IList<ResourcePack> availablePacks,
    ISet<ResourcePack> loadedPacks)
    : ListBoxTemplateResource<ResourcePack> {
    public const string GlobalId = "layouts/templates/resourcePackItem";

    private readonly GameScreen _screen = screen;
    private readonly IList<ResourcePack> _availablePacks = availablePacks;
    private readonly ISet<ResourcePack> _loadedPacks = loadedPacks;

    public override void Preload() {
        base.Preload();
        AddLayout("resourcePackItem");
        AddElement<Button>("toggle");
        AddElement<Button>("up");
        AddElement<Button>("down");
    }

    private class Template : BasicTemplate {
        private readonly ResourcePackSelectorTemplate _resource;
        private int _index;
        private ResourcePack _item;
        private bool _loaded;

        public Template(ResourcePackSelectorTemplate resource) : base(resource) {
            _resource = resource;

            Button toggleButton = GetElement<Button>("toggle");
            toggleButton.onClick += (_, _) => {
                bool canUnload = _resource._loadedPacks.Count > 1 &&
                    _item.name != resources.defaultPackName;
                if(!canUnload && _loaded)
                    return;

                if(_loaded)
                    _resource._loadedPacks.Remove(_item);
                else
                    _resource._loadedPacks.Add(_item);
                _resource._screen.UpdatePacks();
            };

            GetElement<Button>("up").onClick += (_, _) => {
                _resource._availablePacks.RemoveAt(_index);
                _resource._availablePacks.Insert(_index + 1, _item);
                _resource._screen.UpdatePacks();
            };

            GetElement<Button>("down").onClick += (_, _) => {
                _resource._availablePacks.RemoveAt(_index);
                _resource._availablePacks.Insert(_index - 1, _item);
                _resource._screen.UpdatePacks();
            };
        }

        public override void UpdateWithItem(int index, ResourcePack item, int width) {
            _index = index;
            _item = item;

            int maxY = _resource._availablePacks.Count - 1;
            int y = maxY - index;

            _loaded = _resource._loadedPacks.Contains(item);

            bool canMoveUp = y > 0 && item.name != resources.defaultPackName;
            bool canMoveDown = y < maxY &&
                _resource._availablePacks[index - 1].name != resources.defaultPackName;

            Button toggleButton = GetElement<Button>("toggle");
            toggleButton.text =
                item.name.Length > toggleButton.size.x ? item.name[..toggleButton.size.x] : item.name;
            toggleButton.toggled = _loaded;

            Button moveUpButton = GetElement<Button>("up");
            moveUpButton.active = canMoveUp;

            Button moveDownButton = GetElement<Button>("down");
            moveDownButton.active = canMoveDown;
        }

        public override void MoveTo(Vector2Int origin, int index, Vector2Int size) {
            int maxY = _resource._availablePacks.Count - 1;
            int y = maxY - index;
            y *= height;
            foreach((string id, Element element) in idElements)
                element.position = _resource.GetElement(id).position + origin + new Vector2Int(0, y);
        }
    }

    public override IListBoxTemplateFactory<ResourcePack>.Template CreateTemplate() => new Template(this);
}
