using UnityEngine;

namespace SupKonQuest
{
    [RequireComponent(typeof(UnitStats))]
    public class UnitSpell : MonoBehaviour
    {
        private UnitStats stats;
        private float spellCooldownTimer;
        private float spellDurationTimer;
        private bool isActive;

        public bool IsActive => isActive;
        public bool IsOnCooldown => spellCooldownTimer > 0f;
        public float CooldownProgress => 1f - Mathf.Clamp01(spellCooldownTimer / stats.spellCooldown);
        public float DurationProgress => Mathf.Clamp01(spellDurationTimer / stats.spellDuration);

        private void Awake()
        {
            stats = GetComponent<UnitStats>();
        }

        private void Update()
        {
            if (!stats.hasActivable) return;

            if (isActive)
            {
                spellDurationTimer -= Time.deltaTime;
                ApplySpellEffect();

                if (spellDurationTimer <= 0f)
                    DeactivateSpell();
            }
            else if (spellCooldownTimer > 0f)
            {
                spellCooldownTimer -= Time.deltaTime;
            }
        }

        public bool TryActivate()
        {
            if (!stats.hasActivable || isActive || spellCooldownTimer > 0f) return false;

            isActive = true;
            spellDurationTimer = stats.spellDuration;
            spellCooldownTimer = 0f;
            return true;
        }

        private void DeactivateSpell()
        {
            isActive = false;
            spellDurationTimer = 0f;
            spellCooldownTimer = stats.spellCooldown;
            RemoveSpellEffect();
        }

        private void ApplySpellEffect()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
            foreach (Collider hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                UnitStats other = hit.GetComponent<UnitStats>();
                if (other == null || other.ownerId != stats.ownerId) continue;

                if (stats.unitType == UnitType.Heal)
                {
                    int healAmount = Mathf.RoundToInt(5f * Time.deltaTime);
                    if (healAmount > 0) other.Heal(healAmount);
                }
                else if (stats.unitType == UnitType.Support)
                {
                    UnitMovement mov = hit.GetComponent<UnitMovement>();
                    if (mov != null)
                        hit.GetComponent<UnityEngine.AI.NavMeshAgent>().speed = other.moveSpeed * 1.5f;

                    UnitAttack atk = hit.GetComponent<UnitAttack>();
                }
            }
        }

        private void RemoveSpellEffect()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
            foreach (Collider hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                UnitStats other = hit.GetComponent<UnitStats>();
                if (other == null || other.ownerId != stats.ownerId) continue;

                if (stats.unitType == UnitType.Support)
                {
                    UnityEngine.AI.NavMeshAgent agent = hit.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null) agent.speed = other.moveSpeed;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!isActive) return;
            Gizmos.color = stats.unitType == UnitType.Heal ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 5f);
        }
    }
}