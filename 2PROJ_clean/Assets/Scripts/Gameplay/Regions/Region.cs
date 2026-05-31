using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    public class Region : MonoBehaviour
    {
        public RegionData data;

        [Header("Zone géographique (world space)")]
        public Vector3 center = Vector3.zero;
        public Vector3 size = new Vector3(10f, 100f, 10f);

        [HideInInspector] public List<Camp> camps = new List<Camp>();
        [HideInInspector] public string nameKey = "region_default";
        [HideInInspector] public int defaultBonusGold = 10;

        public bool ContainsPoint(Vector3 point)
        {
            Vector3 half = size * 0.5f;
            Vector3 c = center;
            return point.x >= c.x - half.x && point.x <= c.x + half.x
                && point.z >= c.z - half.z && point.z <= c.z + half.z;
        }

        public PlayerData GetOwner()
        {
            if (camps.Count == 0) return null;
            PlayerData first = camps[0].owner;
            if (first == null) return null;
            foreach (Camp c in camps)
                if (c.owner != first) return null;
            return first;
        }

        public bool IsOwnedBy(PlayerData player)
        {
            if (player == null || camps.Count == 0) return false;
            foreach (Camp c in camps)
                if (c.owner != player) return false;
            return true;
        }

        public string GetDisplayName()
        {
            string key = (data != null && !string.IsNullOrEmpty(data.regionNameKey))
                ? data.regionNameKey : nameKey;
            return LocalizationManager.Get(key);
        }

        public int GetBonusGold() => (data != null && data.bonusGold > 0) ? data.bonusGold : defaultBonusGold;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
