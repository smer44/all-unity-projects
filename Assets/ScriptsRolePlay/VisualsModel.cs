using System;
using UnityEngine;


[Serializable]
public class GameCompactUnit
{
    public float size; 
    public Vector2 location;
}

[Serializable]
public class CreatureUnit
{
    CreatureVisuals visuals;
    

    UnitPersonality personality;

    public EquipmentPiece[] equipment;

    Action currentAction;

}


[Serializable]
public class CreatureVisuals
{
    //public float distance; // Normalized distance: 0.5 is about 10 meters.
    // Normalized size: 0.5 is human size, 1 is gigantic, 0 is very tiny.
    //public float visibleMass;
    public float postureConfidence;
    public float aggressionAppearance;
    public float fearAppearance;

    //public float visibleEquipment;- moved to equipment
    
    //public float armorCoverage;
    //public float weaponReadiness;
    public float visibleUnitClassDanger;
    public float raceDanger;
    public float visualPassiveDanger;
    public float woundedAppearance;
    public float deadAppearance;
    public float attractiveness;
    public float genderAppearance; // 1 is male, 0 is female.
    public float visualCreatureAge; // 0.5 is middle age around 35.
    //public float socialStatusAppearance;
}

// has : gesture, action, emotion

public class CreatureVisualStats
{
    public float athletism;
    public float nimbleness;

    public float raceDanger;

}


public class CreatureVisualBehavior
{
    
}

[Serializable]
public class CreatureStats
{
    public float strength;
    public float dexterity;
    public float intelligence;
    public float perception;
}


[Serializable]
public class ThingVisuals
{
    public float distance; // Normalized distance: 0.5 is about 10 meters.
    public float objectSize; // Normalized size: 0.5 is a carryable object, 1 is very large.
    public float visibleMass;
    public float portability;
    public float materialHardness;
    public float sharpness;
    public float heatAppearance;
    public float energySignature;
    public float mechanismComplexity;
    public float activationState;
    public float stability;
    public float damaged; // Towards 1 means heavier visible damage.
    public float containerAppearance;
    public float openedContainerAppearance;
    public float toolAppearance;
    public float weaponAppearance;
    public float readableAppearance;
    public float keyAppearance;
    public float valueAppearance;
    public float suspiciousAppearance;
    public float cleanliness;
}

[Serializable]
public class LocationVisuals
{
    public float distance; // Normalized distance to the location or its important part.
    public float areaSize;
    public float openness;
    public float navigability;
    public float crowding;
    public float lighting;
    public float visibility;
    public float coverAvailability;
    public float concealmentOpportunities;
    public float exitVisibility;
    public float securityPresence;
    public float hostileTerritoryAppearance;
    public float structuralDamage;
    public float heightDanger;
    public float fallDanger;
    public float fireDanger;
    public float toxicDanger;
    public float magicalOrTechnologicalActivity;
    public float resourceAppearance;
    public float comfortAppearance;
}
