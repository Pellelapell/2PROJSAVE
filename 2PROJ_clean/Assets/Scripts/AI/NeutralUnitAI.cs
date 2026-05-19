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

        [Header("Défense")]
        [Tooltip("Distance max depuis la position d'origine (leash). 0 = automatique (3× attackRange)")]
        public float leashRange = 0f;

        private UnitStats stats;
        private NavMeshAgent agent;
        private float attackCooldown;
        private Vector3 homePosition;
        private Camp guardedCamp;
        private bool initialized;

        private void Awake()
        {
            stats = GetComponent<UnitStats>();
            agent = GetComponent<NavMeshAgent>();

            // Fallback si le masque n'est pas assigné dans le prefab
            if (unitLayerMask == 0) unitLayerMask = ~0;

            // homePosition dès Awake pour avoir une valeur valide même si la coroutine échoue
            homePosition = transform.position;
        }

        // Appelé par HexGridGenerator juste après Instantiate pour injecter le camp directement
        public void SetGuardedCamp(Camp camp) => guardedCamp = camp;

        private void Start()
        {
            if (stats != null && stats.ownerId != 0) { enabled = false; return; }
            if (stats != null) stats.ownerId = 0;

            var ua = GetComponent<UnitAttack>();
            if (ua != null) ua.enabled = false;
            var um = GetComponent<UnitMovement>();
            if (um != null) um.enabled = false;

            // leashRange avant la coroutine pour éviter un leash=0
            if (leashRange <= 0f)
                leashRange = stats != null ? stats.attackRange * 3f : 6f;

            if (agent != null)
                StartCoroutine(InitAgentOnNavMesh());
            else
                initialized = true;

            // Fallback : si SetGuardedCamp n'a pas été appelé au spawn
            if (guardedCamp == null)
                guardedCamp = FindNearestNeutralCamp();

            if (guardedCamp == null)
                Debug.LogWarning($"[NeutralUnitAI] {name} : aucun camp neutre trouvé !", this);
        }

        private IEnumerator InitAgentOnNavMesh()
        {
            yield return null; // attend un frame que le NavMesh soit prêt

            int walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");
            if (walkableMask <= 0) walkableMask = NavMesh.AllAreas;

            agent.enabled = false;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, walkableMask))
                transform.position = hit.position;
            agent.enabled = true;

            homePosition = transform.position;

            float speed = stats != null ? stats.moveSpeed * 0.7f : 2f;
            agent.speed            = speed;
            agent.angularSpeed     = 360f;
            agent.stoppingDistance = stats != null ? stats.attackRange * 0.85f : 0.5f;

            initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;
            if (stats == null || stats.currentHealth <= 0) return;

            attackCooldown -= Time.deltaTime;
            bool canMove = agent != null && agent.isOnNavMesh;

            // Retry si le camp n'était pas encore prêt au Start
            if (guardedCamp == null)
                guardedCamp = FindNearestNeutralCamp();

            // ── Priorité 1 : ennemi près du camp gardé → avancer pour engager ──────
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

            // ── Priorité 2 : ennemi à portée d'attaque → frappe sur place ───────────
            UnitStats inRange = FindEnemyInRange(stats.attackRange);
            if (inRange != null)
            {
                if (canMove) StopMoving();
                TryAttack(inRange);
                return;
            }

            // ── Priorité 3 : aucune menace → retour à la position d'origine ────────
            if (canMove)
            {
                float distHome = Vector3.Distance(transform.position, homePosition);
                if (distHome > agent.stoppingDistance + 0.1f)
                    MoveTo(homePosition);
                else
                    StopMoving();
            }
        }

        // ── Attaque ─────────────────────────────────────────────────────────────

        private void TryAttack(UnitStats target)
        {
            if (attackCooldown > 0f) return;
            if (stats.isAOE) PerformAOEAttack(target.transform.position);
            else             target.TakeDamage(stats.attackDamage);
            attackCooldown = 1f / Mathf.Max(0.01f, stats.attackSpeed);
        }

        private void PerformAOEAttack(Vector3 impactPoint)
        {
            Collider[] hits = Physics.OverlapSphere(impactPoint, stats.aoeRadius, unitLayerMask);
            foreach (Collider hit in hits)
            {
                UnitStats other = hit.GetComponent<UnitStats>();
                if (other == null || other.ownerId == 0 || other.currentHealth <= 0) continue;
                float dist = Vector3.Distance(impactPoint, other.transform.position);
                other.TakeAOEDamage(stats.attackDamage, dist, stats.aoeRadius);
            }
        }

        // ── Détection ────────────────────────────────────────────────────────────

        private UnitStats FindEnemyInRange(float range)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, range, unitLayerMask);
            UnitStats closest = null;
            float closestDist = float.MaxValue;
            foreach (Collider hit in hits)
            {
                UnitStats other = hit.GetComponent<UnitStats>();
                if (other == null || other.ownerId == 0 || other.currentHealth <= 0) continue;
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
                if (other == null || other.ownerId == 0 || other.currentHealth <= 0) continue;
                float dist = Vector3.Distance(guardedCamp.transform.position, other.transform.position);
                if (dist < closestDist) { closestDist = dist; closest = other; }
            }
            return closest;
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

        // ── Mouvement ────────────────────────────────────────────────────────────

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
