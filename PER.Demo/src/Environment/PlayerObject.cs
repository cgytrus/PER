using System;

using PER.Abstractions;
using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Demo.Environment;

public class PlayerObject : LevelObject, IUpdatable, ITickable, ILight {
    public override int layer => 0;
    public override RenderCharacter character { get; } = new('@', Color.transparent, new Color(0, 255, 255, 255));
    public override bool blocksLight => false;

    public Color3 color => new(0f, 0f, 0f);
    public byte emission => 0;
    public byte reveal => 8;

    private int _moveX;
    private int _moveY;

    public void Update(TimeSpan time) {
        _moveX = 0;
        _moveY = 0;

        if(input.KeyPressed(KeyCode.D))
            _moveX++;
        if(input.KeyPressed(KeyCode.A))
            _moveX--;
        if(input.KeyPressed(KeyCode.S))
            _moveY++;
        if(input.KeyPressed(KeyCode.W))
            _moveY--;
    }

    // shtu up
    // ReSharper disable once CognitiveComplexity
    public void Tick(TimeSpan time) {
        int moveX = _moveX;
        int moveY = _moveY;

        bool isDiagonal = moveX != 0 && moveY != 0;
        bool collidesHorizontal = moveX != 0 && level.HasObjectAt<WallObject>(position + new Vector2Int(moveX, 0));
        bool collidesVertical = moveY != 0 && level.HasObjectAt<WallObject>(position + new Vector2Int(0, moveY));
        bool collidesDiagonal = isDiagonal && level.HasObjectAt<WallObject>(position + new Vector2Int(moveX, moveY));

        if(isDiagonal &&
            (collidesHorizontal && collidesVertical || !collidesHorizontal && !collidesVertical && collidesDiagonal)) {
            moveX = 0;
            moveY = 0;
        }
        else if(!isDiagonal || collidesDiagonal) {
            if(collidesHorizontal)
                moveX = 0;
            if(collidesVertical)
                moveY = 0;
        }

        position += new Vector2Int(moveX, moveY);
        Vector2Int cameraPosition = level.LevelToCameraPosition(position);
        if(Math.Abs(cameraPosition.x) > 5)
            level.cameraPosition += new Vector2Int(moveX, 0);
        if(Math.Abs(cameraPosition.y) > 5)
            level.cameraPosition += new Vector2Int(0, moveY);
    }
}
