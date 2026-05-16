using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    [RequireComponent(typeof(UnitStats))]
    public class UnitSpell : MonoBehaviour
    {
        private UnitStats stats;
        private float spellCooldownTimer;
        private float spellDurationTimer;
        private bool isActive;

        // Heal : tick toutes les 0.5s pour éviter les problèmes de deltaTime
        private float healTimer;

        // Support : liste des unités buffées pour pouvoir retirer le buff proprement
        private readonly List<UnitStats> buffedUnits = new List<UnitStats>();

        public bool IsActive => isActive;
        public bool IsOnCooldown => spellCooldownTimer > 0f;
        public float CooldownProgress => stats != null ? 1f - Mathf.Clamp01(spellCooldownTimer / stats.spellCooldown) : 0f;
        public float DurationProgress => stats != null ? Mathf.Clamp01(spellDurationTimer / stats.spellDuration) : 0f;

        private void Awake()
        {
            stats = GetComponent<UnitStats>();
        }

        private void Update()
        {
            if (stats == null || !stats.hasActivable) return;

            if (isActive)
            {
                spellDurationTimer -= Time.deltaTime;

                if (stats.unitType == UnitType.Heal)
                {
                    healTimer += Time.deltaTime;
                    if (healTimer >= 0.5f)
                    {
                        healTimer = 0f;
                        ApplyHealTick();
                    }
                }

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
            if (stats == null || !stats.hasActivable || isActive || spellCooldownTimer > 0f) return false;

            isActive = true;
            spellDurationTimer = stats.spellDuration;
            healTimer = 0f;

            if (stats.unitType == UnitType.Support)
                ApplySupportBuff();

            return true;
        }

        private void DeactivateSpell()
        {
            isActive = false;
            spellDurationTimer = 0f;
            spellCooldownTimer = stats.spellCooldown;

            if (stats.unitType == UnitType.Support)
                RemoveSupportBuff();

            buffedUnits.Clear();
        }

        // ── Support ──────────────────────────────────────────────────

        private void ApplySupportBuff()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
            foreach (Collider hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                UnitStats other = hit.GetComponentInParent<UnitStats>();
                if (other == null || other.ownerId != stats.ownerId) continue;

                other.attackSpeedMultiplier = 1.5f;

                NavMeshAgent agent = hit.GetComponentInParent<NavMeshAgent>();
                if (agent != null) agent.speed = other.moveSpeed * 1.5f;

                if (!buffedUnits.Contains(other))
                    buffedUnits.Add(other);
            }
        }

        private void RemoveSupportBuff()
        {
            foreach (UnitStats unit in buffedUnits)
            {
                if (unit == null) continue;
                unit.attackSpeedMultiplier = 1f;

                NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
                if (agent != null) agent.speed = unit.moveSpeed;
            }
        }

        // ── Heal ─────────────────────────────────────────────────────

        private void ApplyHealTick()
        {
            // 10 HP toutes les 0.5s = 20 HP/s
            Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
            foreach (Collider hit in hits)
            {
                UnitStats other = hit.GetComponentInParent<UnitStats>();
                if (other == null || other.ownerId != stats.ownerId) continue;
                other.Heal(10);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!isActive || stats == null) return;
            Gizmos.color = stats.unitType == UnitType.Heal ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 5f);
        }
    }
}
