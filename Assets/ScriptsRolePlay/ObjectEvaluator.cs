using UnityEngine;

public class ObjectEvaluator
{
    float sizeSignificance = 1f;
    float distanceSignificance = 1f;
    float locationSignificance = 0.8f;
    float visualAthletismFactor = 1f;
    float visualEquipmentFactor = 1f;
    public Action actionWeights = new Action
    {
        aggressiveMovement = 0.6f,
        threateningPosture = 0.7f,
        attackAction = 1f,
        destructiveAction = 0.5f,
        blockingAction = 0.4f,
        hostileCommunication = 0.6f,
        refusalCommunication = 0.3f,
        fearCommunication = 0f,
        friendlyCommunication = -0.5f,
        helpingAction = -0.7f,
        surrenderAction = -0.8f
    };

    // 0 - known, 1 - unknown.
    public float Familiarity(GameUnit self, GameUnit unit)
    {
        return 1f;
    }

    public float Distance(GameUnit unit, GameUnit other)
    {
        return (unit.location - other.location).magnitude;
    }

    // High significance is about location you are currently in, big object size, if object is near you.
    public float Significance(GameUnit evaluator, GameUnit unit)
    {
        //if (unit.IsLocation())
        //{
        //    return locationSignificance;
        //}

        return -Distance(evaluator,unit) * distanceSignificance + unit.size * sizeSignificance;
    }

    public float PowerfullAppearance(GameUnit self, GameUnit unit)
    {
        return unit.visibleAthletism * visualAthletismFactor + EstimateEquipmentPowerAll(self,unit) * visualEquipmentFactor;
    }

    public float EstimateEquipmentPower(GameUnit evaluator, EquipmentPiece piece)
    {
        if (evaluator == null || piece == null)
        {
            return 0f;
        }

        if (evaluator.equipmentKnowledge > piece.technologyLevel)
        {
            return piece.power;
        }

        return piece.apearance;
    }

    public float EstimateEquipmentPowerAll(GameUnit evaluator, GameUnit other)
    {
        if (other == null || other.equipment == null)
        {
            return 0f;
        }

        float equipmentPower = 0f;
        foreach (EquipmentPiece piece in other.equipment)
        {
            equipmentPower += EstimateEquipmentPower(evaluator, piece);
        }

        return equipmentPower;
    }
    
    public float HostileBehavior(Action action)
    {

        if (action == null)
        {
            return 0f;
        }

        float hostileBehavior =
            action.aggressiveMovement * actionWeights.aggressiveMovement +
            action.threateningPosture * actionWeights.threateningPosture +
            action.attackAction * actionWeights.attackAction +
            action.destructiveAction * actionWeights.destructiveAction +
            action.blockingAction * actionWeights.blockingAction +
            action.hostileCommunication * actionWeights.hostileCommunication +
            action.refusalCommunication * actionWeights.refusalCommunication +
            action.fearCommunication * actionWeights.fearCommunication +
            action.friendlyCommunication * actionWeights.friendlyCommunication +
            action.helpingAction * actionWeights.helpingAction +
            action.surrenderAction * actionWeights.surrenderAction;

        return hostileBehavior;
    }

    public float KnownPower(GameUnit unit)
    {
        return 0.5f;
    }

    public float HostileAppearance(GameUnit unit)
    {
        return 0.5f;//unit.raceDanger;
    }

    public float HostileRelationship(GameUnit unit)
    {
        return 0.5f;
    }

    // IsDangerousUnknown = isHostileAppearance * isPowerfullAppearance
    // IsDangerousKnown = max(hostile actions, known hostile attitude) * known power
    // dangerEstimate = IsDangerousUnknown * IsUnknown + IsDangerousKnown * (1 - IsUnknown)
    // friendly / hostile appearance = max(friendly / hostile actions, harmless / dangerous look)
    // empathy influences if you want to attack or not.
    // target selection: +order, +danger, -empathy.
    public float DangerEstimateOfUnit(GameUnit self, GameUnit unit)
    {
        float significance = Significance(self,unit);
        float isKnown = Familiarity(self, unit);
        float powerfullAppearance = PowerfullAppearance(self,unit);
        float hostileAppearance = HostileAppearance(unit);
        float hostileBehavior = HostileBehavior(unit.action);
        float hostileRelationships = HostileRelationship(unit);
        float knownPower = KnownPower(unit);

        float dangerByAppearance = (hostileAppearance + hostileBehavior) * powerfullAppearance;
        float dangerByKnowledge = Mathf.Max(hostileBehavior, hostileRelationships) * knownPower;
        float dangerEstimate = dangerByAppearance * (1 - isKnown) + dangerByKnowledge * isKnown;

        return dangerEstimate;
    }

    // Returns estimation how ready is current unit to make harm another unit.
    public float MasterObedinessEstimate(GameUnit unit)
    {
        return 0.5f;
    }

    // what is the model of making/forcing unit to dosomething what would includealso master as object 
    //they evaluate?

    //order * loyality to somebody who made the order

    // other is in danger + high loyality 

    // reputation of somebody who commands (both another unit or master) 

    // d n d order/ chaotical, good / evil ? 

    

}



// personality traits, put 0 .. 1, where 0.5 is middle value 
public class UnitPersonality
{
    float althruism; // selfish / althruistic 

    float curiousity; // Incurious / curious

    float discipline;

    float autonomy;

    float ambitious;

        
    float courage;

    float activeness; 

}

public class UnitConversationalCapabilities
{
    public float speech;
    public float mannersKnowledge;

    public float politeness;

    public float arrogance;


}
