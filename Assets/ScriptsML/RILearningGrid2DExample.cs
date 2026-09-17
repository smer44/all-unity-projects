using UnityEngine;
using System.Globalization;
using System.Text;

public class RILearningGrid2DExample :  MonoBehaviour
{

    public RIQLearning<Vector2Int, MoveOnGrid2D> iQLearning = new RIQLearning<Vector2Int, MoveOnGrid2D>();
    public int steps = 1000;
    private int n =0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        n =0;
        iQLearning.states = new RIStatePositionOnGrid2D();
        iQLearning.actions = new RIRandomAllowedAction();
        iQLearning.quality = new RIQuality<Vector2Int, MoveOnGrid2D>();
        iQLearning.epsilon = 0.2f;
        iQLearning.gammaDecay = 0.95f;
        iQLearning.alfa = 0.1f;


    }

    // Update is called once per frame
    void Update()
    {   
        if(n < steps)
        {
            iQLearning.UpdateChain();
            n++;
        }
        else
        {
            Debug.Log(PPGrid(iQLearning.quality, iQLearning.states as RIStatePositionOnGrid2D));
            n = 0;
        }
        
    }

    public string PPGrid(RIQuality<Vector2Int, MoveOnGrid2D> quality, RIStatePositionOnGrid2D gridStates)
    {
        if (gridStates == null)
        {
            return string.Empty;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine( $"Size: {quality.q.Count}");

        for (int y = gridStates.minBounds.y; y <= gridStates.maxBounds.y; y++)
        {
            if (y > gridStates.minBounds.y)
            {
                sb.AppendLine();
            }

            for (int x = gridStates.minBounds.x; x <= gridStates.maxBounds.x; x++)
            {
                if (x > gridStates.minBounds.x)
                {
                    sb.Append(" ");
                }

                float value = quality.BestValueInState(new Vector2Int(x, y));
                sb.Append(value.ToString("0.00", CultureInfo.InvariantCulture));
            }
        }

        return sb.ToString();
    }
}
