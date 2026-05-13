using UnityEngine;

namespace SupKonQuest
{
    [CreateAssetMenu(menuName = "SupKonQuest/Data/Region")]
    public class RegionData : ScriptableObject
    {
        public string regionNameKey = "region_default";
        public int bonusGold = 50;
        public UnitType bonusUnitType = UnitType.Infantry;
        public int bonusUnitCount = 2;
    }
}
