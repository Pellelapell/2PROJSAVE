namespace SupKonQuest
{
    public enum MapType
    {
        Classic,
        FrozenPeaks,
        Island
    }

    public enum HexTerrain
    {
        Walkable,
        Mountain,
        Water
    }

    public enum CampType
    {
        Normal,
        Port,
        NeutralSpecial,
        Castle
    }

    public enum Race
    {
        Human,
        Demon,
        Elf
    }

    public enum BuildingType
    {
        Camp,
        Sawmill,
        Port,
        Castle
    }

    public enum UnitType
    {
        Infantry,
        Support,
        Heal,
        Range,
        Heavy,
        AntiArmor,
        Mortar,

        Transport,
        Frigate,
        Destroyer
    }

    public enum DamageType
    {
        Physical,
        Piercing,
        Siege,   
        Healing
    }
}
