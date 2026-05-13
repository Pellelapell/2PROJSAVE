using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    // Composant à ajouter sur le prefab Transport (UnitType.Transport).
    // Permet d'embarquer des unités alliées et de les déplacer sur l'eau.
    // Si le transport coule, les passagers sont détruits.
    [RequireComponent(typeof(UnitStats))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class TransportShip : MonoBehaviour
    {
        [Header("Transport")]
        public int capacity = 6;

        private readonly List<UnitStats> passengers = new List<UnitStats>();
        private UnitStats stats;

        public int PassengerCount => passengers.Count;
        public bool IsFull => passengers.Count >= capacity;
        public bool IsEmpty => passengers.Count == 0;

        private void Awake()
        {
            stats = GetComponent<UnitStats>();
        }

        private void Start()
        {
            stats.OnDeath += OnShipSunk;
        }

        private void OnDestroy()
        {
            if (stats != null) stats.OnDeath -= OnShipSunk;
        }

        // Appel depuis InputManager quand le joueur droit-clique sur ce transport avec des unités sélectionnées
        public bool Embark(UnitStats unit)
        {
            if (unit == null || IsFull) return false;
            if (unit.ownerId != stats.ownerId) return false;
            if (passengers.Contains(unit)) return false;

            passengers.Add(unit);

            // Cacher l'unité sans la détruire
            unit.gameObject.SetActive(false);

            NavMeshAgent unitAgent = unit.GetComponent<NavMeshAgent>();
            if (unitAgent != null) unitAgent.enabled = false;

            return true;
        }

        // Appel depuis InputManager quand le joueur presse E (ou droit-clique au sol depuis le transport)
        public void DisembarkAll(Vector3 targetPosition)
        {
            foreach (UnitStats unit in passengers)
            {
                if (unit == null) continue;

                // Placer l'unité autour du point de débarquement
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
            // Tous les passagers périssent avec le bateau
            foreach (UnitStats unit in passengers)
            {
                if (unit != null)
                    Destroy(unit.gameObject);
            }
            passengers.Clear();
        }

        // Retourne un string pour l'UI : "Passagers : 3/6"
        public string GetPassengerLabel()
        {
            return $"{LocalizationManager.Get("transport_passengers")} : {passengers.Count}/{capacity}";
        }
    }
}
