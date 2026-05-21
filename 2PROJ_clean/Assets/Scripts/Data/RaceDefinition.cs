using UnityEngine;
using SupKonQuest;

[CreateAssetMenu(menuName = "SupKonQuest/Data/Race Definition")]
public class RaceDefinition : ScriptableObject
{
    public Race race;
    public string displayName;
    public Color uiColor = Color.white;

    [System.Serializable]
    public struct UnitSkinEntry
    {
        public UnitType unitType;
        public Mesh     mesh;
        public Material material;
    }

    [System.Serializable]
    public struct BuildingSkinEntry
    {
        public BuildingType buildingType; // Camp, Sawmill, Port, Castle
        public Mesh         mesh;
        public Material     material;
    }

    [Header("Unit Skins (par type)")]
    public UnitSkinEntry[] unitSkins;

    [Header("Building Skins (Camp / Sawmill / Port / Castle)")]
    public BuildingSkinEntry[] buildingSkins;

    public UnitSkinEntry? GetUnitSkin(UnitType type)
    {
        for (int i = 0; i < unitSkins.Length; i++)
            if (unitSkins[i].unitType == type)
                return unitSkins[i];
        return null;
    }

    public BuildingSkinEntry? GetBuildingSkin(BuildingType type)
    {
        for (int i = 0; i < buildingSkins.Length; i++)
            if (buildingSkins[i].buildingType == type)
                return buildingSkins[i];
        return null;
    }

    public Material GetUnitMaterial(UnitType type) => GetUnitSkin(type)?.material;
}
