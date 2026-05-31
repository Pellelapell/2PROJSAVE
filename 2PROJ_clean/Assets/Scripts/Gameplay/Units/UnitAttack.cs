using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    [RequireComponent(typeof(UnitStats))]
    public class UnitAttack : MonoBehaviour
    {
        private NavMeshAgent agent;
        private UnitStats    stats;
        private float        attackCooldown;

        private UnitStats      currentUnitTarget;
        private Camp           currentCampTarget;
        private BuildingHealth currentBuildingTarget;

        private bool hasReachedRange;

        public bool IsAttacking { get; private set; }
        public UnitStats CurrentTarget     => currentUnitTarget;
        public Camp      CurrentCampTarget => currentCampTarget;

        private void Awake()
        {
            stats = GetComponent<UnitStats>();
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (stats == null) return;
            if (stats.attackDamage <= 0) { IsAttacking = false; return; }

            attackCooldown -= Time.deltaTime;
            IsAttacking = false;

            if (currentUnitTarget != null)
            {
                if (currentUnitTarget.currentHealth <= 0) { currentUnitTarget = null; return; }
                HandleUnitTarget();
                return;
            }
            if (currentCampTarget != null)
            {
                if (!IsEnemyCamp(currentCampTarget)) { currentCampTarget = null; return; }
                HandleCampTarget();
                return;
            }
            if (currentBuildingTarget != null)
            {
                if (currentBuildingTarget.currentHP <= 0) { currentBuildingTarget = null; return; }
                HandleBuildingTarget();
                return;
            }

            UnitMovement mov = GetComponent<UnitMovement>();
            if (mov != null && (mov.HasPlayerMoveOrder || mov.IsLocked)) return;

            UnitStats autoEnemy = FindClosestEnemyInRange(stats.detectRange);
            if (autoEnemy != null) HandleAutoAttack(autoEnemy);
        }


        public void SetUnitTarget(UnitStats target)
        {
            currentUnitTarget     = target;
            currentCampTarget     = null;
            currentBuildingTarget = null;
            hasReachedRange       = false;
        }

        public void SetCampTarget(Camp camp)
        {
            currentCampTarget     = camp;
            currentUnitTarget     = null;
            currentBuildingTarget = null;
            hasReachedRange       = false;
        }

        public void SetBuildingTarget(BuildingHealth building)
        {
            currentBuildingTarget = building;
            currentUnitTarget     = null;
            currentCampTarget     = null;
            hasReachedRange       = false;
        }

        public void ClearTargets()
        {
            currentUnitTarget     = null;
            currentCampTarget     = null;
            currentBuildingTarget = null;
            hasReachedRange       = false;
        }

        public void TriggerCounterAttack(UnitStats attacker)
        {
            if (attacker == null || attacker.currentHealth <= 0) return;
            if (currentUnitTarget != null) return;
            UnitMovement mov = GetComponent<UnitMovement>();
            if (mov != null && (mov.HasPlayerMoveOrder || mov.IsLocked)) return;
            if (mov != null) mov.Stop();
            SetUnitTarget(attacker);
        }


        private void HandleUnitTarget()
        {
            float dist = Vector3.Distance(transform.position, currentUnitTarget.transform.position);

            if (dist <= stats.attackRange)
            {
                hasReachedRange = true;
                StopMoving();
                FaceTarget(currentUnitTarget.transform);
                IsAttacking = true;
                if (attackCooldown <= 0f)
                {
                    attackCooldown = 1f / Mathf.Max(0.01f, stats.attackSpeed * stats.attackSpeedMultiplier);
                    PerformAttackOnUnit(currentUnitTarget);
                }
            }
            else if (!hasReachedRange && agent != null && agent.isOnNavMesh)
            {
                agent.stoppingDistance = stats.attackRange * 0.9f;
                agent.isStopped = false;
                if (Vector3.Distance(agent.destination, currentUnitTarget.transform.position) > 0.5f)
                    agent.SetDestination(currentUnitTarget.transform.position);
            }
        }


        private void HandleCampTarget()
        {
            float dist = Vector3.Distance(transform.position, currentCampTarget.transform.position);
            if (dist <= stats.attackRange)
            {
                StopMoving();
                FaceTarget(currentCampTarget.transform);
                IsAttacking = true;
                if (attackCooldown <= 0f)
                {
                    attackCooldown = 1f / Mathf.Max(0.01f, stats.attackSpeed * stats.attackSpeedMultiplier);
                    currentCampTarget.TakeDamage(Mathf.RoundToInt(stats.attackDamage * RegionMultiplier()), stats);
                }
            }
            else
            {
                MoveAgent(currentCampTarget.transform.position);
            }
        }


        private void HandleBuildingTarget()
        {
            float dist = Vector3.Distance(transform.position, currentBuildingTarget.transform.position);
            if (dist <= stats.attackRange)
            {
                StopMoving();
                FaceTarget(currentBuildingTarget.transform);
                IsAttacking = true;
                if (attackCooldown <= 0f)
                {
                    attackCooldown = 1f / Mathf.Max(0.01f, stats.attackSpeed * stats.attackSpeedMultiplier);
                    currentBuildingTarget.TakeDamage(Mathf.RoundToInt(stats.attackDamage * RegionMultiplier()));
                }
            }
            else
            {
                MoveAgent(currentBuildingTarget.transform.position);
            }
        }


        private void HandleAutoAttack(UnitStats target)
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= stats.attackRange)
            {
                StopMoving();
                FaceTarget(target.transform);
                IsAttacking = true;
                if (attackCooldown <= 0f)
                {
                    attackCooldown = 1f / Mathf.Max(0.01f, stats.attackSpeed * stats.attackSpeedMultiplier);
                    PerformAttackOnUnit(target);
                }
            }
            else
            {
                MoveAgent(target.transform.position);
            }
        }

        private UnitStats FindClosestEnemyInRange(float range)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, range);
            UnitStats closest = null;
            float closestDist = float.MaxValue;
            foreach (Collider c in hits)
            {
                UnitStats other = c.GetComponentInParent<UnitStats>();
                if (other == null || other == stats) continue;
                if (other.ownerId == stats.ownerId || other.ownerId == GameConstants.NEUTRAL_ID) continue;
                if (other.currentHealth <= 0) continue;
                UnitMovement otherMov = other.GetComponent<UnitMovement>();
                if (otherMov != null && otherMov.IsLocked) continue;
                float d = Vector3.Distance(transform.position, other.transform.position);
                if (d < closestDist) { closestDist = d; closest = other; }
            }
            return closest;
        }


        private void PerformAttackOnUnit(UnitStats target)
        {
            AudioManager.Instance?.PlayAttack();

            int damage = stats.attackDamage;
            if (stats.unitType == UnitType.AntiArmor && target.unitType == UnitType.Heavy)
                damage *= 2;
            damage = Mathf.RoundToInt(damage * RegionMultiplier());

            if (stats.isAOE)
            {
                Collider[] hits = Physics.OverlapSphere(target.transform.position, stats.aoeRadius);
                foreach (Collider c in hits)
                {
                    UnitStats other = c.GetComponentInParent<UnitStats>();
                    if (other == null || other.ownerId == stats.ownerId || other.currentHealth <= 0) continue;
                    float d = Vector3.Distance(target.transform.position, other.transform.position);
                    other.TakeAOEDamageFrom(Mathf.RoundToInt(damage), d, stats.aoeRadius, stats);
                }
            }
            else
            {
                target.TakeDamageFrom(damage, stats);
            }
        }


        private bool IsEnemyCamp(Camp camp)
        {
            if (camp == null) return false;
            if (camp.isNeutral) return true;
            return camp.owner != null && camp.owner.playerId != stats.ownerId;
        }

        private float RegionMultiplier()
        {
            if (RegionManager.Instance == null) return 1f;
            return RegionManager.Instance.IsInOwnedRegion(transform.position, stats.ownerId) ? 1.2f : 1f;
        }

        private void MoveAgent(Vector3 destination)
        {
            if (agent == null || !agent.isOnNavMesh) return;
            agent.stoppingDistance = stats.attackRange * 0.9f;
            agent.isStopped = false;
            agent.SetDestination(destination);
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
