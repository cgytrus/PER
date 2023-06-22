using System;

using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Demo.Environment;

public class PlayerObject : LevelObject {
    protected override RenderCharacter character { get; } = new('@', Color.transparent, new Color(0, 255, 255, 255));

    private int _moveX;
    private int _moveY;

    public override void Update(TimeSpan time) {
        //level.cameraPosition = position;

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
    public override void Tick(TimeSpan time) {
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
    }
}
