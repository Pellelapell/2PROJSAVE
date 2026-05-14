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
            public UnitDefinition def;
            public GameObject prefab;
            public float timeLeft;
        }

        private readonly Queue<QueueEntry> queue = new Queue<QueueEntry>();
        private QueueEntry current;

        public int  GetQueueCount()    => queue.Count + (current != null ? 1 : 0);
        public bool IsProducing()      => current != null;
        public UnitType? CurrentType() => current?.type;

        public float GetProgress01()
        {
            if (current == null || current.def == null) return 0f;
            float buildTime = current.def.buildTime;
            return buildTime <= 0f ? 1f : 1f - (current.timeLeft / buildTime);
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

            UnitDefinition def = unitDatabase != null ? unitDatabase.Get(type) : null;
            if (def == null)
            {
                Debug.LogWarning($"[Production] Pas de définition pour {type}");
                return false;
            }

            GameObject prefab = GetPrefab(type);
            if (prefab == null)
            {
                Debug.LogWarning($"[Production] Pas de prefab pour {type}");
                return false;
            }

            if (!camp.owner.SpendMoney(def.price))
            {
                Debug.Log("[Production] Pas assez d'argent");
                return false;
            }

            var entry = new QueueEntry { type = type, def = def, prefab = prefab, timeLeft = def.buildTime };

            if (current == null) current = entry;
            else queue.Enqueue(entry);

            return true;
        }

        public void SpawnUnitInstant(UnitType type)
        {
            if (camp == null || camp.owner == null) return;
            UnitDefinition def = unitDatabase?.Get(type);
            if (def == null) return;
            GameObject prefab = GetPrefab(type);
            if (prefab == null) return;
            DoSpawn(new QueueEntry { type = type, def = def, prefab = prefab });
        }

        // ── Spawn ────────────────────────────────────────────────────

        private void DoSpawn(QueueEntry entry)
        {
            if (camp == null || camp.owner == null || entry.prefab == null) return;

            // Position de départ : le camp lui-même (on laisse UnitMovement.InitOnNavMesh gérer le placement)
            Vector3 spawnPos = transform.position;
            if (camp.spawnPoint != null)
                spawnPos = camp.spawnPoint.position;

            // Désactiver le NavMeshAgent avant l'instantiation pour éviter le placement automatique raté
            GameObject unitObj = Instantiate(entry.prefab, spawnPos, Quaternion.identity);

            // Configurer les stats
            UnitStats stats = unitObj.GetComponent<UnitStats>();
            if (stats != null)
            {
                stats.ownerId = camp.owner.playerId;
                stats.race    = camp.owner.race;
                stats.InitFromDefinition(entry.def);
            }

            // Appliquer les visuels de race
            UnitVisuals visuals = unitObj.GetComponent<UnitVisuals>();
            if (visuals != null) visuals.ApplyRaceVisuals();

            // Déléguer le placement NavMesh à UnitMovement
            UnitMovement mov = unitObj.GetComponent<UnitMovement>();
            float speed = stats != null ? stats.moveSpeed : 3.5f;
            if (mov != null)
                mov.InitOnNavMesh(speed);
            else
            {
                // Fallback si UnitMovement absent
                NavMeshAgent agent = unitObj.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.enabled = false;
                    if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 30f, NavMesh.AllAreas))
                        unitObj.transform.position = hit.position;
                    agent.enabled = true;
                    agent.speed = speed;
                }
            }
        }

        private GameObject GetPrefab(UnitType type)
        {
            foreach (var e in availableUnits)
                if (e.unitType == type) return e.prefab;
            return null;
        }
    }
}
