using UnityEngine;
using SupKonQuest;

[CreateAssetMenu(menuName = "SupKonQuest/Data/Race Definition")]
public class RaceDefinition : ScriptableObject
{
    public Race race;
    public string displayName;
    public Color uiColor = Color.white;

    [System.Serializable]
    public struct UnitVisual
    {
        public UnitType unitType;
        public Sprite sprite;
    }

    [Header("Visuals per UnitType")]
    public UnitVisual[] visuals;

    public Sprite GetSprite(UnitType type)
    {
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i].unitType == type)
                return visuals[i].sprite;

        return null;
    }
}
//selem
