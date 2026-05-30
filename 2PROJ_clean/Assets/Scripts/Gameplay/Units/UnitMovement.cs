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

        public bool IsLocked { get; set; }

        public bool HasPlayerMoveOrder { get; private set; }

        private static Material selectionCircleMat;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            stats  = GetComponent<UnitStats>();

            if (selectionCircle == null)
                selectionCircle = CreateSelectionCircle();

            SetSelected(false);
        }

        private GameObject CreateSelectionCircle()
        {
            if (selectionCircleMat == null)
            {
                Shader sh = Shader.Find("Standard")
                         ?? Shader.Find("Universal Render Pipeline/Lit");
                selectionCircleMat = new Material(sh);
                selectionCircleMat.color = new Color(0f, 0.9f, 0f, 0.35f);

                selectionCircleMat.SetFloat("_Mode", 3);
                selectionCircleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                selectionCircleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                selectionCircleMat.SetInt("_ZWrite", 0);
                selectionCircleMat.EnableKeyword("_ALPHABLEND_ON");
                selectionCircleMat.renderQueue = 3000;
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "SelectionCircle";
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            go.transform.localScale    = new Vector3(0.8f, 0.01f, 0.8f);
            go.GetComponent<Renderer>().sharedMaterial = selectionCircleMat;
            Destroy(go.GetComponent<Collider>());
            return go;
        }

        private void Update()
        {
            if (HasPlayerMoveOrder && !IsMoving && !hasPending)
                HasPlayerMoveOrder = false;

            if (!hasPending) return;
            if (!agent.isOnNavMesh) return;

            agent.isStopped = false;
            agent.SetDestination(pendingDestination);
            hasPending = false;
        }

        public void InitOnNavMesh(float speed)
        {
            int waterAreaIndex = NavMesh.GetAreaFromName("Water");
            int areaMask       = ComputeAreaMask(waterAreaIndex);

            agent.enabled = false;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, areaMask))
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

        public void MoveTo(Vector3 destination)
        {
            if (IsLocked) return;
            HasPlayerMoveOrder = true;
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

        private int ComputeAreaMask(int waterAreaIndex)
        {
            if (IsNavalUnit())
            {
                return waterAreaIndex >= 0 ? (1 << waterAreaIndex) : NavMesh.AllAreas;
            }

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
