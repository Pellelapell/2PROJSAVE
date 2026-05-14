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

        /// <summary>Place l'agent sur le NavMesh et applique la vitesse. Appeler juste après Instantiate.</summary>
        public void InitOnNavMesh(float speed)
        {
            StartCoroutine(PlaceOnNavMesh(speed));
        }

        public void MoveTo(Vector3 destination)
        {
            pendingDestination = destination;
            hasPending = true;

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
            // Attendre que le NavMesh soit prêt (1 frame)
            yield return null;

            agent.enabled = false;

            // Chercher le point NavMesh le plus proche dans un grand rayon
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 30f, NavMesh.AllAreas))
                transform.position = hit.position;

            agent.enabled = true;
            agent.speed = speed;

            // Appliquer la destination en attente si elle existe
            if (hasPending && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(pendingDestination);
                hasPending = false;
            }
        }
    }
}
