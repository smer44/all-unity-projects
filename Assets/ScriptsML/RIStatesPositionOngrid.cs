using UnityEngine;


public enum MoveOnGrid2D
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3
}



public class RIStatePositionOnGrid2D  : RIStateChanger<Vector2Int, MoveOnGrid2D>
{

    public Vector2Int startState = Vector2Int.zero;

    public Vector2Int endState = new Vector2Int (4,4);

    public Vector2Int minBounds = Vector2Int.zero;

    public Vector2Int maxBounds = new Vector2Int(4, 4);
    
    private readonly Vector2Int[] directions =
    {
        new Vector2Int(0, -1), // Up
        new Vector2Int(0, 1),  // Down
        new Vector2Int(-1, 0), // Left
        new Vector2Int(1, 0)   // Right
    };

    private readonly MoveOnGrid2D[] allAllowed = {MoveOnGrid2D.Up, MoveOnGrid2D.Down, MoveOnGrid2D.Left, MoveOnGrid2D.Right};


    public override MoveOnGrid2D[]  AllowedActions(Vector2Int state)
    {
        return allAllowed;
    }

    public override Vector2Int NextState(Vector2Int prev, MoveOnGrid2D action)
    {
        return prev + directions[(int)action];
    }

    public override Vector2Int StartState()
    {
        return startState;
    }

    public override Vector2Int EndState()
    {
        return endState;
    }

    public override float ImmediateReward(Vector2Int prev, Vector2Int next)
    {
        if (!IsInBounds(next))
        {
            return -10f;
        }

        return next == endState? 10f: -1f;
    }


    public override bool IsEndState(Vector2Int state)
    {
        return state == endState;
    }

    public bool IsInBounds(Vector2Int position)
    {
        return position.x >= minBounds.x
            && position.y >= minBounds.y
            && position.x <= maxBounds.x
            && position.y <= maxBounds.y;
    }

    public override bool IsAllowed(Vector2Int state)
    {
        return IsInBounds(state);
    }

}
