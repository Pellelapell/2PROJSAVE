using System.Collections.Generic;
using UnityEngine;

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

        [Header("Unites disponibles")]
        public List<UnitProductionEntry> availableUnits = new List<UnitProductionEntry>();

        private struct QueuedUnit
        {
            public UnitType type;
            public UnitDefinition definition;
            public GameObject prefab;
        }

        private readonly Queue<QueuedUnit> productionQueue = new Queue<QueuedUnit>();
        private QueuedUnit currentProduction;
        private float currentProductionTimer;
        private bool isProducing;

        private void Awake()
        {
            if (camp == null)
                camp = GetComponent<Camp>();
        }

        private void Update()
        {
            if (!isProducing) return;

            currentProductionTimer -= Time.deltaTime;
            if (currentProductionTimer <= 0f)
            {
                SpawnUnit(currentProduction);
                StartNextProduction();
            }
        }

        public bool Produce(UnitType type)
        {
            if (camp == null || camp.owner == null) return false;

            UnitDefinition def = unitDatabase != null ? unitDatabase.Get(type) : null;
            if (def == null) { Debug.LogWarning($"[CampProduction] Pas de definition pour {type}"); return false; }

            GameObject prefab = GetPrefab(type);
            if (prefab == null) { Debug.LogWarning($"[CampProduction] Pas de prefab pour {type}"); return false; }

            if (!camp.owner.SpendMoney(def.price)) { Debug.Log("[CampProduction] Pas assez d'argent"); return false; }

            productionQueue.Enqueue(new QueuedUnit { type = type, definition = def, prefab = prefab });

            if (!isProducing) StartNextProduction();
            return true;
        }

        public int GetQueueCount() => productionQueue.Count + (isProducing ? 1 : 0);
        public bool IsProducing() => isProducing;
        public UnitType? GetCurrentUnitType() => isProducing ? currentProduction.type : (UnitType?)null;

        public float GetCurrentProgress01()
        {
            if (!isProducing || currentProduction.definition == null) return 0f;
            float buildTime = currentProduction.definition.buildTime;
            return buildTime <= 0f ? 1f : 1f - (currentProductionTimer / buildTime);
        }

        private void StartNextProduction()
        {
            if (productionQueue.Count == 0) { isProducing = false; currentProduction = default; currentProductionTimer = 0f; return; }
            currentProduction = productionQueue.Dequeue();
            currentProductionTimer = currentProduction.definition != null ? currentProduction.definition.buildTime : 5f;
            isProducing = true;
        }

        private void SpawnUnit(QueuedUnit queued)
        {
            if (camp.owner == null || camp.spawnPoint == null || queued.prefab == null) return;

            // Snap spawn position to NavMesh surface
            Vector3 spawnPos = camp.spawnPoint.position;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                spawnPos = hit.position;

            GameObject unitObj = Instantiate(queued.prefab, spawnPos, Quaternion.identity);

            UnitStats stats = unitObj.GetComponent<UnitStats>();
            if (stats != null)
            {
                stats.ownerId = camp.owner.playerId;
                stats.race = camp.owner.race;
                stats.InitFromDefinition(queued.definition);
            }

            UnitVisuals visuals = unitObj.GetComponent<UnitVisuals>();
            if (visuals != null) visuals.ApplyRaceVisuals();

            UnityEngine.AI.NavMeshAgent agent = unitObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                if (stats != null) agent.speed = stats.moveSpeed;
                // Force onto NavMesh if not already placed correctly
                if (!agent.isOnNavMesh && UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit warpHit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                    agent.Warp(warpHit.position);
            }
        }

        // Spawn instantané sans coût ni queue (utilisé pour les bonus de région)
        public void SpawnUnitInstant(UnitType type)
        {
            if (camp == null || camp.owner == null) return;
            UnitDefinition def = unitDatabase != null ? unitDatabase.Get(type) : null;
            if (def == null) return;
            GameObject prefab = GetPrefab(type);
            if (prefab == null) return;
            SpawnUnit(new QueuedUnit { type = type, definition = def, prefab = prefab });
        }

        private GameObject GetPrefab(UnitType type)
        {
            foreach (UnitProductionEntry e in availableUnits)
                if (e.unitType == type) return e.prefab;
            return null;
        }
    }
}