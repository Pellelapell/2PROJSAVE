using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    // Ajouter ce composant sur les prefabs d'unités neutres.
    // Désactive le mouvement et UnitAttack → l'unité reste sur place et attaque en zone.
    [RequireComponent(typeof(UnitStats))]
    public class NeutralUnitAI : MonoBehaviour
    {
        [Header("Layers")]
        public LayerMask unitLayerMask;

        private UnitStats stats;
        private float attackCooldown;

        private void Awake()
        {
            stats = GetComponent<UnitStats>();
        }

        private void Start()
        {
            // Si une unité joueur a ce composant par erreur (prefab partagé), s'auto-désactiver
            if (stats != null && stats.ownerId != 0) { enabled = false; return; }

            // Forcer l'identité neutre — jamais sélectionnable par un joueur
            if (stats != null) stats.ownerId = 0;

            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            UnitAttack regularAttack = GetComponent<UnitAttack>();
            if (regularAttack != null) regularAttack.enabled = false;

            UnitMovement mov = GetComponent<UnitMovement>();
            if (mov != null) mov.enabled = false;
        }

        private void Update()
        {
            if (stats == null || stats.currentHealth <= 0) return;

            attackCooldown -= Time.deltaTime;
            if (attackCooldown > 0f) return;

            UnitStats target = FindTarget();
            if (target == null) return;

            if (stats.isAOE)
                PerformAOEAttack(target.transform.position);
            else
                target.TakeDamage(stats.attackDamage);

            attackCooldown = 1f / Mathf.Max(0.01f, stats.attackSpeed);
        }

        private UnitStats FindTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, stats.attackRange, unitLayerMask);
            UnitStats closest = null;
            float closestDist = float.MaxValue;

            foreach (Collider hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                UnitStats other = hit.GetComponent<UnitStats>();
                // Les unités neutres (ownerId == 0) attaquent tous les joueurs réels
                if (other == null || other.ownerId == 0 || other.currentHealth <= 0) continue;
                float dist = Vector3.Distance(transform.position, other.transform.position);
                if (dist < closestDist) { closestDist = dist; closest = other; }
            }
            return closest;
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

        private void OnDrawGizmosSelected()
        {
            if (stats == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stats.attackRange);
        }
    }
}
