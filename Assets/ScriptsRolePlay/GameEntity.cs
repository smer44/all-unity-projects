using UnityEngine;


public class GameEntity
{
    

}


public class GameUnit //compact game entity
{
    public Vector3 location;
    public float size;

    public float visibleAthletism;

    public EquipmentPiece[] equipment;

    public float equipmentKnowledge;

    public Action action;
}





public class GameUnitOld 
{

    public BasicUnitData basic;

    public CreatureVisuals creatureVisuals;
    public ThingVisuals thingVisuals;
    public LocationVisuals locationVisuals;

    UnitPersonality personality;

    Action action;

    public float knowledge;

    public EquipmentPiece[] equipment;


    

    public bool IsLocation()
    {
        return basic != null && basic.isLocation;
    }

    public bool IsCreature()
    {
        return basic != null && basic.isCreature;
    }

    public bool IsThing()
    {
        return basic != null && basic.isThing;
    }



    public float VisibleUnitClassDanger()
    {
        if (creatureVisuals == null)
        {
            return 0f;
        }

        return creatureVisuals.visibleUnitClassDanger;
    }

    public Action CurrentAction()
    {
        return action;
    }

    public float Knowledge()
    {
        return knowledge;
    }

    public EquipmentPiece[] Equipment()
    {
        return equipment;
    }

}

[System.Serializable]
public class EquipmentPiece
{
    public string name;
    public float technologyLevel;
    public float apearance;
    public float power;
}

[System.Serializable]
public class BasicUnitData
{
    

    public bool isLocation;
    public bool isCreature;
    public bool isThing;
}





[System.Serializable]
public class Action
{
    public float aggressiveMovement; // Rushing, chasing, closing distance fast.
    public float threateningPosture; // Raised weapon, combat stance, aiming.
    public float attackAction; // Currently attacking or preparing attack.
    public float destructiveAction; // Breaking objects, damaging environment.
    public float blockingAction; // Blocking path, guarding, surrounding.
    public float hostileCommunication; // Insults, threats, intimidation.
    public float refusalCommunication; // Refusing help, rejecting negotiation.
    public float fearCommunication; // Panic, warning others, screaming.
    public float friendlyCommunication; // Greeting, apology, calming gesture.
    public float helpingAction; // Healing, protecting, opening path.
    public float surrenderAction; // Hands up, dropped weapon, fleeing from conflict.
}


[System.Serializable]
public class RelationshipToMaster
{
    public float knowingMasterExistence;

    public float masterAuthority;
    

}



// Capabilities known from lore, prior knowledge, identification, or inner
// structure rather than from appearance alone.
[System.Serializable]
public class RealCapability
{
    public float mass; // Normalized mass: 0.5 is average human weight around 70 kg.

    public float agentive;
    public float isCreature;
    public float isCreatureDead;
    public float isIntelligent;
    public float isSelfMobile;

    public float isHandTool; // Tool is something you can hold in hand and use.
    public float isHandWeapon;
    public float isContainer; // Container is something you can put something in.
    public float isTable; // Table has a flat surface convenient for putting something on top.
    public float isDrink;
    public float isFood;
    public float isPremises;
    public float isConsole;
    public float isObstacle;
    public float isWalkableSurface;
    public float isVehicle;
    public float isFuel;
    public float isIngot;
    public float isReadable;
    public float isKey; // Something used to unlock something.
    public float isPowerSocket;

    public float relativeBattlePower; // If it is a unit: 0.5 is equal, 1 is extremely high.
    public float hostility; // 0.5 is neutral.

    public float acquainted;
}


public class InputOutputModel : MonoBehaviour
{
    // Anxiety rises when visual capability is high-significance but real
    // capability is unknown.
    public CreatureVisuals creatureVisuals = new CreatureVisuals();
    public ThingVisuals thingVisuals = new ThingVisuals();
    public LocationVisuals locationVisuals = new LocationVisuals();
    public RealCapability realCapability = new RealCapability();

    // Danger is relativeBattlePower + hostility.


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
