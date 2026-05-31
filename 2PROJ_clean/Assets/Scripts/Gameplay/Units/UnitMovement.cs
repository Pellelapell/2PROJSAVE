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
        public bool HasAttackMoveOrder { get; private set; }

        private GameObject rangeIndicator;
        private static Material selectionCircleMat;
        private static Material rangeIndicatorMat;
        private static Material unitRingMat;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            stats  = GetComponent<UnitStats>();

            if (IsNavalUnit() && agent != null)
                agent.updateRotation = false;

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

        private void Start()
        {
            CreateRangeIndicator();

            UnitStats us = GetComponent<UnitStats>();
            if (us != null && GameManager.Instance != null && us.ownerId == GameManager.Instance.localPlayerId)
                CreateUnitRing();
        }

        private void Update()
        {
            if (HasPlayerMoveOrder && !IsMoving && !hasPending)
                HasPlayerMoveOrder = false;

            if (HasAttackMoveOrder && !IsMoving && !hasPending)
                HasAttackMoveOrder = false;

            if (hasPending && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(pendingDestination);
                hasPending = false;
            }

            RotateNavalUnit();
        }

        private void RotateNavalUnit()
        {
            if (!IsNavalUnit() || agent == null || agent.velocity.sqrMagnitude < 0.01f) return;
            Vector3 dir = agent.velocity;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion target = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 90f * Time.deltaTime);
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
            if (IsNavalUnit())
                agent.updateRotation = false;

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
            HasAttackMoveOrder = false;
            MoveToForced(destination);
        }

        public void AttackMoveTo(Vector3 destination)
        {
            if (IsLocked) return;
            HasAttackMoveOrder = true;
            HasPlayerMoveOrder = false;
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
            if (rangeIndicator != null)
                rangeIndicator.SetActive(selected);
        }

        private void CreateRangeIndicator()
        {
            UnitStats s = GetComponent<UnitStats>();
            float range = s != null ? s.attackRange : 0f;
            if (range <= 0f) return;

            if (rangeIndicatorMat == null)
            {
                Shader sh = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                rangeIndicatorMat = new Material(sh);
                rangeIndicatorMat.color = new Color(1f, 1f, 1f, 0.18f);
                rangeIndicatorMat.SetFloat("_Mode", 3);
                rangeIndicatorMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                rangeIndicatorMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                rangeIndicatorMat.SetInt("_ZWrite", 0);
                rangeIndicatorMat.EnableKeyword("_ALPHABLEND_ON");
                rangeIndicatorMat.renderQueue = 3000;
            }

            rangeIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rangeIndicator.name = "RangeIndicator";
            rangeIndicator.transform.SetParent(transform);
            rangeIndicator.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            float d = range * 2f;
            rangeIndicator.transform.localScale = new Vector3(d, 0.004f, d);
            rangeIndicator.GetComponent<Renderer>().sharedMaterial = rangeIndicatorMat;
            Destroy(rangeIndicator.GetComponent<Collider>());
            rangeIndicator.SetActive(false);
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

        private void CreateUnitRing()
        {
            if (unitRingMat == null)
            {
                Shader sh = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                unitRingMat = new Material(sh);
                unitRingMat.SetFloat("_Mode", 3);
                unitRingMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                unitRingMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                unitRingMat.SetInt("_ZWrite", 0);
                unitRingMat.EnableKeyword("_ALPHABLEND_ON");
                unitRingMat.renderQueue = 3000;
            }

            GameObject ringObj = new GameObject("UnitRing");
            ringObj.transform.SetParent(transform);
            ringObj.transform.localPosition = Vector3.zero;
            ringObj.transform.localRotation = Quaternion.identity;

            LineRenderer lr = ringObj.AddComponent<LineRenderer>();
            lr.useWorldSpace             = false;
            lr.loop                      = true;
            lr.widthMultiplier           = 0.05f;
            lr.shadowCastingMode         = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows            = false;
            lr.material                  = unitRingMat;

            Color ringColor = new Color(0.45f, 0.78f, 1f, 0.65f);
            lr.startColor = ringColor;
            lr.endColor   = ringColor;

            const int   segments = 32;
            const float radius   = 0.48f;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0.04f, Mathf.Sin(a) * radius));
            }
        }
    }
}
