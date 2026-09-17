using UnityEngine;


public class RIRandomAllowedAction: RIActionChooser<Vector2Int, MoveOnGrid2D>
{

    public override MoveOnGrid2D ChooseRandomAction(RIStateChanger<Vector2Int, MoveOnGrid2D> states, Vector2Int state)
    {
        var allowed = states.AllowedActions(state);
        var index = Random.Range(0,allowed.Length);

        return allowed[index];

    }

}
