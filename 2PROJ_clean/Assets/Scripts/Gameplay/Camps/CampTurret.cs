using UnityEngine;

namespace SupKonQuest
{
    public class CampTurret : MonoBehaviour
    {
        [Header("Stats")]
        public float range = 8f;
        public int damage = 20;
        public float attackCooldown = 2f;

        [Header("Layers")]
        public LayerMask unitLayerMask;

        private Camp camp;
        private float cooldownTimer;

        private void Awake()
        {
            camp = GetComponent<Camp>();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.gameOver) return;

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f) return;

            UnitStats target = FindTarget();
            if (target == null) return;

            target.TakeDamage(damage);
            cooldownTimer = attackCooldown;
        }

        private UnitStats FindTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, range, unitLayerMask);
            UnitStats closest = null;
            float closestDist = float.MaxValue;

            foreach (Collider hit in hits)
            {
                UnitStats unit = hit.GetComponent<UnitStats>();
                if (unit == null || unit.currentHealth <= 0) continue;

                bool isEnemy = camp.isNeutral
                    ? unit.ownerId != 0
                    : camp.owner != null && unit.ownerId != camp.owner.playerId;

                if (!isEnemy) continue;

                float dist = Vector3.Distance(transform.position, unit.transform.position);
                if (dist < closestDist) { closestDist = dist; closest = unit; }
            }

            return closest;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
