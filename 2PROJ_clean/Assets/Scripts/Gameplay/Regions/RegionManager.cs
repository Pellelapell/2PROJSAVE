using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    // Exécution après HexGridGenerator (-10) et GameManager (-5) mais avant le reste
    [DefaultExecutionOrder(0)]
    public class RegionManager : MonoBehaviour
    {
        public static RegionManager Instance { get; private set; }

        private Region[] regions;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            regions = FindObjectsByType<Region>(FindObjectsSortMode.None);
        }

        // Appelé par HexGridGenerator après avoir spawné tous les camps
        public void AssignCampsToRegions()
        {
            // Vider les listes existantes
            foreach (Region r in regions)
                r.camps.Clear();

            Camp[] allCamps = FindObjectsByType<Camp>(FindObjectsSortMode.None);
            foreach (Camp camp in allCamps)
            {
                foreach (Region r in regions)
                {
                    if (r.ContainsPoint(camp.transform.position))
                    {
                        r.camps.Add(camp);
                        break; // un camp appartient à une seule région
                    }
                }
            }
        }

        public Region[] GetAllRegions() => regions;

        public List<Region> GetRegionsOwnedBy(PlayerData player)
        {
            List<Region> owned = new List<Region>();
            foreach (Region r in regions)
                if (r.IsOwnedBy(player)) owned.Add(r);
            return owned;
        }

        public int GetRegionBonusGold(PlayerData player)
        {
            int total = 0;
            foreach (Region r in regions)
                if (r.IsOwnedBy(player) && r.data != null) total += r.data.bonusGold;
            return total;
        }

        public Camp GetBonusSpawnCamp(Region region, PlayerData player)
        {
            foreach (Camp c in region.camps)
                if (c.owner == player) return c;
            return null;
        }
    }
}
