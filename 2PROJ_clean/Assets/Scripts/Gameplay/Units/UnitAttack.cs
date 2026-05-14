using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    [RequireComponent(typeof(UnitStats))]
    public class UnitAttack : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField] private LayerMask unitLayerMask;
        [SerializeField] private LayerMask campLayerMask;

        private NavMeshAgent agent;
        private UnitStats stats;
        private float attackCooldown;

        private UnitStats currentUnitTarget;
        private Camp currentCampTarget;
        private bool manualCampTarget;

        private void Awake()
        {
            stats = GetComponent<UnitStats>();
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (stats == null) return;

            attackCooldown -= Time.deltaTime;

            if (currentUnitTarget == null || currentUnitTarget.currentHealth <= 0)
                currentUnitTarget = FindClosestEnemyUnit();

            if (currentUnitTarget != null)
            {
                HandleUnitTarget();
                return;
            }

            // Auto-découverte seulement si pas de cible manuelle
            if (!manualCampTarget && (currentCampTarget == null || !IsEnemyCamp(currentCampTarget)))
                currentCampTarget = FindClosestEnemyCamp();

            if (currentCampTarget != null)
                HandleCampTarget();
        }

        // ── Unit targeting ──────────────────────────────────────────

        private void HandleUnitTarget()
        {
            float dist = Vector3.Distance(transform.position, currentUnitTarget.transform.position);

            if (dist <= stats.attackRange)
            {
                StopMoving();
                FaceTarget(currentUnitTarget.transform);
                if (attackCooldown <= 0f)
                {
                    PerformAttackOnUnit(currentUnitTarget);
                    attackCooldown = 1f / Mathf.Max(0.01f, stats.attackSpeed * stats.attackSpeedMultiplier);
                }
            }
            else if (dist <= stats.detectRange)
            {
                MoveToward(currentUnitTarget.transform.position);
            }
            else
            {
                currentUnitTarget = null;
            }
        }

        private UnitStats FindClosestEnemyUnit()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, stats.detectRange, unitLayerMask);
            UnitStats closest = null;
            float closestDist = float.MaxValue;

            foreach (Collider hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                UnitStats other = hit.GetComponent<UnitStats>();
                if (other == null || other.ownerId == stats.ownerId || other.currentHealth <= 0) continue;
                float dist = Vector3.Distance(transform.position, other.transform.position);
                if (dist < closestDist) { closestDist = dist; closest = other; }
            }
            return closest;
        }

        private void PerformAttackOnUnit(UnitStats target)
        {
            if (target == null) return;

            int damage = stats.attackDamage;
            if (stats.unitType == UnitType.AntiArmor && target.unitType == UnitType.Heavy)
                damage *= 2;

            // +20% dégâts si l'unité combat dans une région lui appartenant
            damage = Mathf.RoundToInt(damage * GetRegionDamageMultiplier());

            if (stats.isAOE)
                PerformAOEAttack(target.transform.position, damage);
            else
                target.TakeDamage(damage);
        }

        private void PerformAOEAttack(Vector3 impactPoint, int baseDamage)
        {
            Collider[] hits = Physics.OverlapSphere(impactPoint, stats.aoeRadius, unitLayerMask);
            foreach (Collider hit in hits)
            {
                UnitStats other = hit.GetComponent<UnitStats>();
                if (other == null || other.ownerId == stats.ownerId || other.currentHealth <= 0) continue;
                float dist = Vector3.Distance(impactPoint, other.transform.position);
                other.TakeAOEDamage(baseDamage, dist, stats.aoeRadius);
            }
        }

        // ── Camp targeting ──────────────────────────────────────────

        private void HandleCampTarget()
        {
            // Annuler si le camp est devenu allié ou inexistant
            if (currentCampTarget == null || !IsEnemyCamp(currentCampTarget))
            {
                currentCampTarget = null;
                manualCampTarget  = false;
                return;
            }

            float dist = Vector3.Distance(transform.position, currentCampTarget.transform.position);

            if (dist <= stats.attackRange)
            {
                StopMoving();
                FaceTarget(currentCampTarget.transform);
                if (attackCooldown <= 0f)
                {
                    int damage = Mathf.RoundToInt(stats.attackDamage * GetRegionDamageMultiplier());
                    currentCampTarget.TakeDamage(damage, stats);
                    attackCooldown = 1f / Mathf.Max(0.01f, stats.attackSpeed * stats.attackSpeedMultiplier);
                }
            }
            else
            {
                // Cible manuelle : toujours avancer. Cible auto : limité au detectRange.
                if (manualCampTarget || dist <= stats.detectRange)
                    MoveToward(currentCampTarget.transform.position);
                else
                    currentCampTarget = null;
            }
        }

        // Ordre manuel depuis InputManager
        public void SetCampTarget(Camp camp)
        {
            currentCampTarget = camp;
            currentUnitTarget = null;
            manualCampTarget  = camp != null;
        }

        private Camp FindClosestEnemyCamp()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, stats.detectRange, campLayerMask);
            Camp closest = null;
            float closestDist = float.MaxValue;

            foreach (Collider hit in hits)
            {
                Camp camp = hit.GetComponent<Camp>();
                if (camp == null || !IsEnemyCamp(camp)) continue;
                float dist = Vector3.Distance(transform.position, camp.transform.position);
                if (dist < closestDist) { closestDist = dist; closest = camp; }
            }
            return closest;
        }

        private bool IsEnemyCamp(Camp camp)
        {
            if (camp == null) return false;
            if (camp.isNeutral) return true;
            return camp.owner != null && camp.owner.playerId != stats.ownerId;
        }

        // ── Region bonus ─────────────────────────────────────────────

        private float GetRegionDamageMultiplier()
        {
            if (RegionManager.Instance == null) return 1f;
            return RegionManager.Instance.IsInOwnedRegion(transform.position, stats.ownerId) ? 1.2f : 1f;
        }

        // ── Helpers ─────────────────────────────────────────────────

        private void MoveToward(Vector3 destination)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(destination);
            }
        }

        private void StopMoving()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        private void FaceTarget(Transform target)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            dir.y = 0f;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        private void OnDrawGizmosSelected()
        {
            if (stats == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stats.attackRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stats.detectRange);
        }
    }
}
