using UnityEngine;
using SupKonQuest;

[CreateAssetMenu(menuName = "SupKonQuest/Data/Unit Database")]
public class UnitDatabase : ScriptableObject
{
    public UnitDefinition[] units;

    public UnitDefinition Get(UnitType type)
    {
        for (int i = 0; i < units.Length; i++)
            if (units[i] != null && units[i].unitType == type)
                return units[i];

        return null;
    }
}

