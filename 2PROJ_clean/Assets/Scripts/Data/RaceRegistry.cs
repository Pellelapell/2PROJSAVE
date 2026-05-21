using UnityEngine;
using SupKonQuest;

// Placer l'asset dans Assets/Resources/RaceRegistry.asset
[CreateAssetMenu(menuName = "SupKonQuest/Data/Race Registry")]
public class RaceRegistry : ScriptableObject
{
    public RaceDefinition[] definitions;

    private static RaceRegistry _instance;

    public static RaceRegistry Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<RaceRegistry>("RaceRegistry");
            return _instance;
        }
    }

    public static RaceDefinition Get(Race race)
    {
        var reg = Instance;
        if (reg == null) return null;
        for (int i = 0; i < reg.definitions.Length; i++)
            if (reg.definitions[i] != null && reg.definitions[i].race == race)
                return reg.definitions[i];
        return null;
    }
}
