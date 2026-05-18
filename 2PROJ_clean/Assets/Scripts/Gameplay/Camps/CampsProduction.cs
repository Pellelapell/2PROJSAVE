using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    public class CampProduction : MonoBehaviour
    {
        [System.Serializable]
        public class UnitProductionEntry
        {
            public UnitType unitType;
            public GameObject prefab;
        }

        [Header("References")]
        public Camp camp;
        public UnitDatabase unitDatabase;

        [Header("Unités disponibles")]
        public List<UnitProductionEntry> availableUnits = new List<UnitProductionEntry>();

        // ── Queue ────────────────────────────────────────────────────

        private class QueueEntry
        {
            public UnitType type;
            public UnitDefinition def;   // peut être null → UnitDefaults utilisé
            public GameObject prefab;
            public float timeLeft;
            public float buildTime;
        }

        private readonly Queue<QueueEntry> queue = new Queue<QueueEntry>();
        private QueueEntry current;

        public int  GetQueueCount()    => queue.Count + (current != null ? 1 : 0);
        public bool IsProducing()      => current != null;
        public UnitType? CurrentType() => current?.type;

        public float GetProgress01()
        {
            if (current == null || current.buildTime <= 0f) return 0f;
            return 1f - (current.timeLeft / current.buildTime);
        }

        // ── Init ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (camp == null) camp = GetComponent<Camp>();
        }

        // ── Update ───────────────────────────────────────────────────

        private void Update()
        {
            if (current == null) return;

            current.timeLeft -= Time.deltaTime;
            if (current.timeLeft <= 0f)
            {
                DoSpawn(current);
                current = queue.Count > 0 ? queue.Dequeue() : null;
            }
        }

        // ── API publique ─────────────────────────────────────────────

        public bool Produce(UnitType type)
        {
            if (camp == null || camp.owner == null) return false;

            // Limite de population : 10 unités par camp possédé
            int popCap = camp.owner.ownedCamps.Count * 10;
            if (CountOwnerUnits() >= popCap)
            {
                Debug.Log("[Production] Limite de population atteinte");
                return false;
            }

            GameObject prefab = GetPrefab(type);
            if (prefab == null)
            {
                Debug.LogWarning($"[Production] Pas de prefab pour {type}");
                return false;
            }

            int price       = UnitDefaults.GetPrice(type);
            float buildTime = UnitDefaults.GetBuildTime(type);

            if (!camp.owner.SpendMoney(price))
            {
                Debug.Log("[Production] Pas assez d'argent");
                return false;
            }

            var entry = new QueueEntry { type = type, def = null, prefab = prefab, timeLeft = buildTime, buildTime = buildTime };

            if (current == null) current = entry;
            else queue.Enqueue(entry);

            return true;
        }

        public int GetPopCap()   => camp?.owner != null ? camp.owner.ownedCamps.Count * 10 : 0;
        public int GetPopCount() => CountOwnerUnits();

        private int CountOwnerUnits()
        {
            if (camp == null || camp.owner == null) return 0;
            int id = camp.owner.playerId;
            int count = 0;
            UnitStats[] all = FindObjectsByType<UnitStats>(FindObjectsSortMode.None);
            foreach (UnitStats us in all)
                if (us.ownerId == id && us.gameObject.activeInHierarchy) count++;
            return count;
        }

        public void SpawnUnitInstant(UnitType type)
        {
            if (camp == null || camp.owner == null) return;
            GameObject prefab = GetPrefab(type);
            if (prefab == null) return;
            float buildTime = UnitDefaults.GetBuildTime(type);
            DoSpawn(new QueueEntry { type = type, def = null, prefab = prefab, buildTime = buildTime });
        }

        // ── Spawn ────────────────────────────────────────────────────

        private void DoSpawn(QueueEntry entry)
        {
            if (camp == null || camp.owner == null || entry.prefab == null) return;

            Vector3 spawnPos = transform.position;
            if (camp.spawnPoint != null)
                spawnPos = camp.spawnPoint.position;

            bool isNaval = entry.type == UnitType.Transport ||
                           entry.type == UnitType.Frigate   ||
                           entry.type == UnitType.Destroyer;

            // Chercher une position valide sur le NavMesh selon le type d'unité
            spawnPos = FindValidSpawnPosition(spawnPos, isNaval);

            GameObject unitObj = Instantiate(entry.prefab, spawnPos, Quaternion.identity);

            UnitStats stats = unitObj.GetComponent<UnitStats>();
            if (stats != null)
            {
                stats.ownerId = camp.owner.playerId;
                stats.race    = camp.owner.race;

                UnitDefaults.Apply(stats, entry.type);
            }

            NeutralUnitAI neutralAI = unitObj.GetComponent<NeutralUnitAI>();
            if (neutralAI != null) neutralAI.enabled = false;

            UnitVisuals visuals = unitObj.GetComponent<UnitVisuals>();
            if (visuals != null) visuals.ApplyRaceVisuals();

            if (stats != null && stats.hasActivable && unitObj.GetComponent<UnitSpell>() == null)
                unitObj.AddComponent<UnitSpell>();

            UnitMovement mov = unitObj.GetComponent<UnitMovement>();
            float speed = stats != null ? stats.moveSpeed : 3.5f;
            if (mov != null)
            {
                mov.InitOnNavMesh(speed);
            }
            else
            {
                NavMeshAgent agent = unitObj.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.enabled = false;
                    int areaMask = isNaval
                        ? NavMesh.GetAreaFromName("Water") >= 0 ? (1 << NavMesh.GetAreaFromName("Water")) : NavMesh.AllAreas
                        : 1 << NavMesh.GetAreaFromName("Walkable");
                    if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 30f, areaMask))
                        unitObj.transform.position = hit.position;
                    agent.enabled = true;
                    agent.speed = speed;
                }
            }
        }

        // Cherche la tuile walkable (ou eau si naval) la plus proche dans un rayon de 15 m.
        // Sur la map Îles, les unités terrestres refusent les îles de moins de 4 tuiles.
        private static Vector3 FindValidSpawnPosition(Vector3 origin, bool naval)
        {
            HexTerrain targetTerrain = naval ? HexTerrain.Water : HexTerrain.Walkable;
            bool islandMap = PlayerPrefs.GetInt("MapType", 0) == 2;
            Collider[] hits = Physics.OverlapSphere(origin, 15f);
            float bestDist = float.MaxValue;
            Vector3 best = origin;

            foreach (Collider c in hits)
            {
                HexTile tile = c.GetComponentInParent<HexTile>();
                if (tile == null || tile.terrain != targetTerrain) continue;

                // Map îles + unité terrestre : ignorer les îles trop petites
                if (!naval && islandMap && BuildingManager.CountIslandSize(tile.transform.position) < 4)
                    continue;

                float d = Vector3.Distance(origin, tile.transform.position);
                if (d < bestDist) { bestDist = d; best = tile.transform.position; }
            }
            return best;
        }

        private GameObject GetPrefab(UnitType type)
        {
            foreach (var e in availableUnits)
                if (e.unitType == type) return e.prefab;
            return null;
        }
    }
}
