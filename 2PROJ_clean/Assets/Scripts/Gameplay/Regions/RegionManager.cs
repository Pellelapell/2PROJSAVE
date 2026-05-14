using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    [DefaultExecutionOrder(0)]
    public class RegionManager : MonoBehaviour
    {
        public static RegionManager Instance { get; private set; }

        [Header("Pool de données (assigné dans l'inspector)")]
        public RegionData[] regionDataPool;

        [Header("Découpage de la map")]
        public int regionsX = 3;
        public int regionsZ = 3;

        private readonly List<Region> regionList = new List<Region>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Génération procédurale ────────────────────────────────────
        // Appelé par HexGridGenerator après avoir calculé MapBounds

        public void GenerateRegions(Bounds mapBounds)
        {
            // Nettoyer les anciennes régions
            foreach (Region r in regionList)
                if (r != null) Destroy(r.gameObject);
            regionList.Clear();

            float zoneW = mapBounds.size.x / regionsX;
            float zoneD = mapBounds.size.z / regionsZ;

            int dataIdx = 0;

            for (int ix = 0; ix < regionsX; ix++)
            {
                for (int iz = 0; iz < regionsZ; iz++)
                {
                    float cx = mapBounds.min.x + (ix + 0.5f) * zoneW;
                    float cz = mapBounds.min.z + (iz + 0.5f) * zoneD;

                    GameObject go = new GameObject($"Region_{ix}_{iz}");
                    go.transform.SetParent(transform);

                    Region r = go.AddComponent<Region>();
                    r.center = new Vector3(cx, 0f, cz);
                    r.size   = new Vector3(zoneW, 200f, zoneD);

                    // Assigner un RegionData du pool (cyclique si pool plus petit)
                    if (regionDataPool != null && regionDataPool.Length > 0)
                        r.data = regionDataPool[dataIdx % regionDataPool.Length];

                    dataIdx++;
                    regionList.Add(r);
                }
            }

            Debug.Log($"[RegionManager] {regionList.Count} régions générées ({regionsX}×{regionsZ}).");
        }

        // ── Assignation des camps aux régions ─────────────────────────

        public void AssignCampsToRegions()
        {
            foreach (Region r in regionList)
                r.camps.Clear();

            Camp[] allCamps = FindObjectsByType<Camp>(FindObjectsSortMode.None);
            foreach (Camp camp in allCamps)
            {
                foreach (Region r in regionList)
                {
                    if (r.ContainsPoint(camp.transform.position))
                    {
                        r.camps.Add(camp);
                        break;
                    }
                }
            }
        }

        // ── API publique ──────────────────────────────────────────────

        public Region[] GetAllRegions() => regionList.ToArray();

        public List<Region> GetRegionsOwnedBy(PlayerData player)
        {
            List<Region> owned = new List<Region>();
            foreach (Region r in regionList)
                if (r.IsOwnedBy(player)) owned.Add(r);
            return owned;
        }

        public int GetRegionBonusGold(PlayerData player)
        {
            int total = 0;
            foreach (Region r in regionList)
                if (r.IsOwnedBy(player) && r.data != null) total += r.data.bonusGold;
            return total;
        }

        public Camp GetBonusSpawnCamp(Region region, PlayerData player)
        {
            foreach (Camp c in region.camps)
                if (c.owner == player) return c;
            return null;
        }

        public bool IsInOwnedRegion(Vector3 position, int ownerId)
        {
            PlayerData player = GameManager.Instance?.GetPlayerById(ownerId);
            if (player == null) return false;
            foreach (Region r in regionList)
                if (r.IsOwnedBy(player) && r.ContainsPoint(position))
                    return true;
            return false;
        }
    }
}
