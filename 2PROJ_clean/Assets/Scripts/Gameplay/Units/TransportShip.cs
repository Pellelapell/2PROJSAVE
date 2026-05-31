using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    [RequireComponent(typeof(UnitStats))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class TransportShip : MonoBehaviour
    {
        [Header("Transport")]
        public int capacity = 6;

        [Header("Auto-embark")]
        [Tooltip("Rayon de dÃ©tection pour l'embarquement automatique (m)")]
        public float autoEmbarkRadius = 1.8f;

        public static event System.Action<List<string>> OnShipSunkWithPassengers;

        private readonly List<UnitStats> passengers = new List<UnitStats>();
        private UnitStats stats;
        private NavMeshAgent agent;

        public int PassengerCount => passengers.Count;
        public bool IsFull => passengers.Count >= capacity;
        public bool IsEmpty => passengers.Count == 0;

        private void Awake()
        {
            stats = GetComponent<UnitStats>();
            agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            stats.OnDeath += OnShipSunk;
        }

        private void OnDestroy()
        {
            if (stats != null) stats.OnDeath -= OnShipSunk;
        }

        private void Update()
        {
            if (!IsFull && agent != null && agent.velocity.sqrMagnitude > 0.01f)
                AutoEmbarkNeighbors();
        }

        private void AutoEmbarkNeighbors()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, autoEmbarkRadius);
            foreach (Collider c in hits)
            {
                if (IsFull) break;
                UnitStats unit = c.GetComponentInParent<UnitStats>();
                if (unit == null || unit == stats) continue;
                if (unit.ownerId != stats.ownerId) continue;
                if (unit.unitType == UnitType.Transport ||
                    unit.unitType == UnitType.Frigate   ||
                    unit.unitType == UnitType.Destroyer) continue;
                if (passengers.Contains(unit)) continue;
                Embark(unit);
            }
        }

        public bool Embark(UnitStats unit)
        {
            if (unit == null || IsFull) return false;
            if (unit.ownerId != stats.ownerId) return false;
            if (passengers.Contains(unit)) return false;

            passengers.Add(unit);
            unit.gameObject.SetActive(false);

            NavMeshAgent unitAgent = unit.GetComponent<NavMeshAgent>();
            if (unitAgent != null) unitAgent.enabled = false;

            return true;
        }

        public void DisembarkAll(Vector3 targetPosition)
        {
            foreach (UnitStats unit in passengers)
            {
                if (unit == null) continue;

                Vector3 offset = Random.insideUnitSphere * 2f;
                offset.y = 0f;
                Vector3 spawnPos = targetPosition + offset;

                unit.transform.position = spawnPos;
                unit.gameObject.SetActive(true);

                NavMeshAgent unitAgent = unit.GetComponent<NavMeshAgent>();
                if (unitAgent != null)
                {
                    unitAgent.enabled = true;
                    if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 10f, ~0))
                        unitAgent.Warp(hit.position);
                }
            }
            passengers.Clear();
        }

        private void OnShipSunk(UnitStats _)
        {
            var names = new List<string>();
            foreach (UnitStats unit in passengers)
            {
                if (unit == null) continue;
                names.Add(UnitDefaults.GetName(unit.unitType));
                Destroy(unit.gameObject);
            }
            passengers.Clear();

            if (names.Count > 0)
                OnShipSunkWithPassengers?.Invoke(names);
        }

        public string GetPassengerLabel()
        {
            return $"{LocalizationManager.Get("transport_passengers")} : {passengers.Count}/{capacity}";
        }
    }
}
