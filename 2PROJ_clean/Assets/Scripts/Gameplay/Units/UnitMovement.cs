using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class UnitMovement : MonoBehaviour
    {
        [Header("Selection Visual")]
        [SerializeField] private GameObject selectionCircle;

        private NavMeshAgent agent;
        private UnitStats stats;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            stats = GetComponent<UnitStats>();
        }

        private void Start()
        {
            if (stats != null)
                agent.speed = stats.moveSpeed;
            SetSelected(false);
        }

        public void MoveTo(Vector3 destination)
        {
            if (agent != null && agent.isOnNavMesh)
                agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (agent != null && agent.isOnNavMesh)
                agent.ResetPath();
        }

        public void SetSelected(bool selected)
        {
            if (selectionCircle != null)
                selectionCircle.SetActive(selected);
        }

        public bool IsMoving =>
            agent != null && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
    }
}