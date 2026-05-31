using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    [RequireComponent(typeof(UnitStats))]
    public class NeutralUnitAI : MonoBehaviour
    {
        [Header("Layers")]
        public LayerMask unitLayerMask;

        [Header("Animation (optionnel)")]
        [Tooltip("ContrÃ´leur d'animation Ã  utiliser. Laissez vide pour prendre celui du DÃ©mon Lourd automatiquement.")]
        public RuntimeAnimatorController fallbackController;

        [Header("DÃ©fense")]
        [Tooltip("Distance max depuis la position d'origine (leash). 0 = automatique (3Ã— attackRange)")]
        public float leashRange = 0f;

        private UnitStats stats;
        private NavMeshAgent agent;
        private float attackCooldown;
        private Vector3 homePosition;
        private Camp guardedCamp;
        private bool initialized;

        private Animator animator;
        private bool animDead;
        private float attackAnimTimer;
        private static readonly int HashIsMoving    = Animator.StringToHash("IsMoving");
        private static readonly int HashIsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int HashIsDead      = Animator.StringToHash("IsDead");

        private void Awake()
        {
            stats = GetComponent<UnitStats>();
            agent = GetComponent<NavMeshAgent>();

            if (unitLayerMask == 0) unitLayerMask = ~0;

            homePosition = transform.position;
        }

        public void SetGuardedCamp(Camp camp) => guardedCamp = camp;

        private void Start()
        {
            if (stats != null && stats.ownerId != 0 && stats.ownerId != GameConstants.NEUTRAL_ID)
            { enabled = false; return; }
            if (stats != null) stats.ownerId = GameConstants.NEUTRAL_ID;

            ShrinkColliders();

            var ua = GetComponent<UnitAttack>();
            if (ua != null) ua.enabled = false;
            var um = GetComponent<UnitMovement>();
            if (um != null) um.enabled = false;

            if (leashRange <= 0f)
                leashRange = stats != null ? stats.attackRange * 3f : 6f;

            if (agent != null)
                InitAgentOnNavMesh();
            else
                initialized = true;

            if (guardedCamp == null)
                guardedCamp = FindNearestNeutralCamp();

            if (guardedCamp == null)
                Debug.LogWarning($"[NeutralUnitAI] {name} : aucun camp neutre trouvÃ© !", this);

            StartCoroutine(FindAnimatorNextFrame());
        }

        private System.Collections.IEnumerator FindAnimatorNextFrame()
        {
            yield return null;
            animator = GetComponentInChildren<Animator>();

            RuntimeAnimatorController ctrl = GetFallbackController();
            if (ctrl == null)
            {
                Debug.LogWarning($"[NeutralUnitAI] {name} : aucun animator controller trouvÃ©. " +
                    "Assignez 'Fallback Controller' dans l'Inspector ou configurez animatorController " +
                    "dans RaceDefinition DÃ©mon > Heavy.", this);
                yield break;
            }

            if (animator == null)
            {
                animator = gameObject.AddComponent<Animator>();
                animator.applyRootMotion = false;
            }
            animator.runtimeAnimatorController = ctrl;
        }

        private RuntimeAnimatorController GetFallbackController()
        {
            if (fallbackController != null) return fallbackController;

            RaceDefinition def = RaceRegistry.Get(Race.Demon);
            if (def == null) return null;
            UnitType lookupType = stats != null ? stats.unitType : UnitType.Heavy;
            var skin = def.GetUnitSkin(lookupType);
            if (!skin.HasValue || skin.Value.animatorController == null)
                skin = def.GetUnitSkin(UnitType.Heavy);
            return skin.HasValue ? skin.Value.animatorController : null;
        }

        private void InitAgentOnNavMesh()
        {
            int walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");
            if (walkableMask <= 0) walkableMask = NavMesh.AllAreas;

            agent.enabled = false;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, walkableMask))
                transform.position = hit.position;
            agent.enabled = true;

            homePosition = transform.position;

            float speed = stats != null ? stats.moveSpeed * 0.7f : 2f;
            agent.speed                  = speed;
            agent.angularSpeed           = 360f;
            agent.stoppingDistance       = stats != null ? stats.attackRange * 0.85f : 0.5f;
            agent.radius                 = 0.3f;
            agent.obstacleAvoidanceType  = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;

            if (animator != null)
            {
                if (!animDead && stats != null && stats.currentHealth <= 0)
                {
                    animDead = true;
                    animator.SetBool(HashIsMoving,    false);
                    animator.SetBool(HashIsAttacking, false);
                    animator.SetBool(HashIsDead,      true);
                }
                else if (!animDead)
                {
                    attackAnimTimer = Mathf.Max(0f, attackAnimTimer - Time.deltaTime);
                    bool isAttacking = attackAnimTimer > 0f;
                    bool isMoving    = agent != null && agent.isOnNavMesh
                                    && !agent.isStopped && agent.hasPath
                                    && agent.remainingDistance > agent.stoppingDistance;
                    animator.SetBool(HashIsAttacking, isAttacking);
                    animator.SetBool(HashIsMoving,    isMoving);
                    animator.speed = (isMoving || isAttacking) ? 1f : 0f;
                }
            }

            if (stats == null || stats.currentHealth <= 0) return;

            attackCooldown -= Time.deltaTime;
            bool canMove = agent != null && agent.isOnNavMesh;

            if (guardedCamp == null)
                guardedCamp = FindNearestNeutralCamp();

            if (guardedCamp != null)
            {
                float scanRange = stats.detectRange > 0f ? stats.detectRange * 2f : stats.attackRange * 4f;
                UnitStats threat = FindEnemyNearCamp(scanRange);
                if (threat != null)
                {
                    float distToThreat = Vector3.Distance(transform.position, threat.transform.position);

                    if (canMove)
                    {
                        float distFromHome = Vector3.Distance(transform.position, homePosition);
                        if (distFromHome < leashRange)
                        {
                            if (distToThreat > stats.attackRange)
                                MoveTo(threat.transform.position);
                            else
                                StopMoving();
                        }
                        else
                        {
                            MoveTo(homePosition);
                            return;
                        }
                    }

                    if (distToThreat <= stats.attackRange)
                        TryAttack(threat);

                    return;
                }
            }

            UnitStats inRange = FindEnemyInRange(stats.attackRange);
            if (inRange != null)
            {
                if (canMove) StopMoving();
                TryAttack(inRange);
                return;
            }

            if (canMove)
            {
                float distHome = Vector3.Distance(transform.position, homePosition);
                if (distHome > agent.stoppingDistance + 0.1f)
                    MoveTo(homePosition);
                else
                    StopMoving();
            }
        }

        private void TryAttack(UnitStats target)
        {
            if (attackCooldown > 0f) return;
            if (stats.isAOE) PerformAOEAttack(target.transform.position);
            else             target.TakeDamageFrom(stats.attackDamage, stats);
            float cooldown = 1f / Mathf.Max(0.01f, stats.attackSpeed);
            attackCooldown  = cooldown;
            attackAnimTimer = Mathf.Min(cooldown * 0.6f, 0.8f);
        }

        private void PerformAOEAttack(Vector3 impactPoint)
        {
            Collider[] hits = Physics.OverlapSphere(impactPoint, stats.aoeRadius, unitLayerMask);
            foreach (Collider hit in hits)
            {
                UnitStats other = hit.GetComponent<UnitStats>();
                if (other == null || other.ownerId == GameConstants.NEUTRAL_ID || other.currentHealth <= 0) continue;
                float dist = Vector3.Distance(impactPoint, other.transform.position);
                other.TakeAOEDamage(stats.attackDamage, dist, stats.aoeRadius);
            }
        }

        private UnitStats FindEnemyInRange(float range)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, range, unitLayerMask);
            UnitStats closest = null;
            float closestDist = float.MaxValue;
            foreach (Collider hit in hits)
            {
                UnitStats other = hit.GetComponent<UnitStats>();
                if (other == null || other.ownerId == GameConstants.NEUTRAL_ID || other.currentHealth <= 0) continue;
                float dist = Vector3.Distance(transform.position, other.transform.position);
                if (dist < closestDist) { closestDist = dist; closest = other; }
            }
            return closest;
        }

        private UnitStats FindEnemyNearCamp(float range)
        {
            if (guardedCamp == null) return null;
            Collider[] hits = Physics.OverlapSphere(guardedCamp.transform.position, range, unitLayerMask);
            UnitStats closest = null;
            float closestDist = float.MaxValue;
            foreach (Collider hit in hits)
            {
                UnitStats other = hit.GetComponent<UnitStats>();
                if (other == null || other.ownerId == GameConstants.NEUTRAL_ID || other.currentHealth <= 0) continue;
                float dist = Vector3.Distance(guardedCamp.transform.position, other.transform.position);
                if (dist < closestDist) { closestDist = dist; closest = other; }
            }
            return closest;
        }

        private void ShrinkColliders()
        {
            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                if (col is CapsuleCollider cc)
                {
                    cc.radius = Mathf.Min(cc.radius, 0.35f);
                    cc.height = Mathf.Min(cc.height, 1.8f);
                }
                else if (col is SphereCollider sc)
                    sc.radius = Mathf.Min(sc.radius, 0.35f);
                else if (col is BoxCollider bc)
                    bc.size = Vector3.Min(bc.size, Vector3.one * 0.7f);
            }
        }

        private Camp FindNearestNeutralCamp()
        {
            Camp[] allCamps = FindObjectsByType<Camp>(FindObjectsSortMode.None);
            Camp nearest = null;
            float nearestDist = float.MaxValue;
            foreach (Camp c in allCamps)
            {
                if (!c.isNeutral) continue;
                float d = Vector3.Distance(transform.position, c.transform.position);
                if (d < nearestDist) { nearestDist = d; nearest = c; }
            }
            return nearest;
        }

        private void MoveTo(Vector3 dest)
        {
            if (agent == null || !agent.isOnNavMesh) return;
            agent.isStopped = false;
            agent.SetDestination(dest);
        }

        private void StopMoving()
        {
            if (agent == null || !agent.isOnNavMesh) return;
            agent.isStopped = true;
            agent.ResetPath();
        }

        private void OnDrawGizmosSelected()
        {
            if (stats == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stats.attackRange);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(homePosition, leashRange > 0f ? leashRange : stats.attackRange * 3f);
            if (guardedCamp != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(guardedCamp.transform.position,
                    stats.detectRange > 0f ? stats.detectRange : stats.attackRange * 3f);
            }
        }
    }
}
