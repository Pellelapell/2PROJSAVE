using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class UnitMovement : MonoBehaviour
    {
        [SerializeField] private GameObject selectionCircle;

        private NavMeshAgent agent;
        private UnitStats stats;

        private Vector3 pendingDestination;
        private bool hasPending;

        public bool IsMoving => agent != null && agent.isOnNavMesh
                             && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

        // Verrouillé pendant la construction : ignore les commandes de mouvement
        public bool IsLocked { get; set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            stats  = GetComponent<UnitStats>();
            SetSelected(false);
        }

        private void Update()
        {
            if (!hasPending) return;
            if (!agent.isOnNavMesh) return;

            agent.isStopped = false;
            agent.SetDestination(pendingDestination);
            hasPending = false;
        }

        // ── API publique ─────────────────────────────────────────────

        public void InitOnNavMesh(float speed)
        {
            StartCoroutine(PlaceOnNavMesh(speed));
        }

        public void MoveTo(Vector3 destination)
        {
            if (IsLocked) return;
            MoveToForced(destination);
        }

        public void MoveToForced(Vector3 destination)
        {
            pendingDestination = destination;
            hasPending = true;

            GetComponent<UnitAttack>()?.ClearTargets();

            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(destination);
                hasPending = false;
            }
        }

        public void Stop()
        {
            hasPending = false;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionCircle != null)
                selectionCircle.SetActive(selected);
        }

        // ── Initialisation NavMesh ────────────────────────────────────

        private IEnumerator PlaceOnNavMesh(float speed)
        {
            yield return null;

            int waterAreaIndex = NavMesh.GetAreaFromName("Water");
            int areaMask       = ComputeAreaMask(waterAreaIndex);

            agent.enabled = false;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 30f, areaMask))
                transform.position = hit.position;

            agent.enabled  = true;
            agent.speed    = speed;
            agent.areaMask = areaMask;

            if (hasPending && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(pendingDestination);
                hasPending = false;
            }
        }

        private int ComputeAreaMask(int waterAreaIndex)
        {
            if (IsNavalUnit())
            {
                // Bateaux : eau uniquement (si l'area Water existe)
                return waterAreaIndex >= 0 ? (1 << waterAreaIndex) : NavMesh.AllAreas;
            }

            // Unités terrestres : tout sauf l'eau
            if (waterAreaIndex >= 0)
                return NavMesh.AllAreas & ~(1 << waterAreaIndex);

            return NavMesh.AllAreas;
        }

        private bool IsNavalUnit()
        {
            if (stats == null) return false;
            return stats.unitType == UnitType.Transport
                || stats.unitType == UnitType.Frigate
                || stats.unitType == UnitType.Destroyer;
        }
    }
}
