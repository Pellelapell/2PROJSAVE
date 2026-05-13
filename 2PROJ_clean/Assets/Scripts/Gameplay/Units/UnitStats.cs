using UnityEngine;
using System;

namespace SupKonQuest
{
    public class UnitStats : MonoBehaviour
    {
        [Header("Identity")]
        public int ownerId;
        public Race race;
        public UnitType unitType;
        public DamageType damageType;

        [Header("Stats")]
        public int maxHealth = 100;
        public int currentHealth = 100;
        public int attackDamage = 10;
        public float attackSpeed = 1f;
        public float attackRange = 2f;
        public float detectRange = 6f;
        public float moveSpeed = 4f;

        [Header("Buffs (runtime)")]
        public float attackSpeedMultiplier = 1f;

        [Header("AOE")]
        public bool isAOE = false;
        public float aoeRadius = 2.5f;
        [Range(0f, 1f)]
        public float aoeFalloff = 0.6f;

        [Header("Activable")]
        public bool hasActivable = false;
        public float spellDuration = 5f;
        public float spellCooldown = 10f;

        [Header("Economy")]
        public int price = 50;

        [Header("UI")]
        public GameObject healthBarPrefab;

        public event Action<UnitStats> OnDeath;

        private UnitHealthBarUI spawnedHealthBar;

        public void InitFromDefinition(UnitDefinition def)
        {
            if (def == null) return;
            unitType = def.unitType;
            price = def.price;
            maxHealth = def.maxHP;
            currentHealth = def.maxHP;
            attackDamage = def.damage;
            attackSpeed = def.attackSpeed;
            attackRange = def.range;
            detectRange = def.range * 1.5f;
            moveSpeed = def.moveSpeed;
            damageType = def.damageType;
            isAOE = def.isAOE;
            aoeRadius = def.aoeRadius;
            aoeFalloff = def.aoeFalloff;
            hasActivable = def.hasActivable;
            spellDuration = def.spellDuration;
            spellCooldown = def.spellCooldown;
            attackSpeedMultiplier = 1f;
        }

        private void Start()
        {
            if (currentHealth <= 0)
                currentHealth = maxHealth;
            CreateHealthBar();
        }

        public void TakeDamage(int amount)
        {
            ApplyDamage(ComputeReceivedDamage(amount));
        }

        public void TakeAOEDamage(int baseDamage, float distanceToCenter, float radius)
        {
            if (radius <= 0f) { TakeDamage(baseDamage); return; }
            float t = Mathf.Clamp01(distanceToCenter / radius);
            float mult = Mathf.Lerp(1f, 1f - aoeFalloff, t);
            int final = ComputeReceivedDamage(Mathf.RoundToInt(baseDamage * mult));
            ApplyDamage(final);
        }

        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        private int ComputeReceivedDamage(int raw)
        {
            // Heavy prend 30% moins de dégâts sauf contre Piercing (Anti-Armor)
            if (unitType == UnitType.Heavy && damageType != DamageType.Piercing)
                return Mathf.RoundToInt(raw * 0.7f);
            // Anti-Armor fait x2 contre Heavy
            return raw;
        }

        private void ApplyDamage(int amount)
        {
            currentHealth = Mathf.Max(currentHealth - amount, 0);
            if (currentHealth <= 0) Die();
        }

        private void Die()
        {
            OnDeath?.Invoke(this);
            if (spawnedHealthBar != null)
                Destroy(spawnedHealthBar.gameObject);
            Destroy(gameObject);
        }

        private void CreateHealthBar()
        {
            if (healthBarPrefab == null) return;
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            GameObject barObj = Instantiate(healthBarPrefab, canvas.transform);
            UnitHealthBarUI bar = barObj.GetComponent<UnitHealthBarUI>();
            if (bar == null) return;
            spawnedHealthBar = bar;
            spawnedHealthBar.Initialize(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRange);
        }
    }
}
