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

        private GameObject targetHighlight;
        private static Material targetHighlightMat;

        private UnitHealthBarUI spawnedHealthBar;

        public void InitFromDefinition(UnitDefinition def)
        {
            if (def == null) return;
            unitType = def.unitType;
            if (def.price > 0)       price       = def.price;
            if (def.maxHP > 0)     { maxHealth   = def.maxHP; currentHealth = def.maxHP; }
            if (def.damage > 0)      attackDamage = def.damage;
            if (def.attackSpeed > 0f) attackSpeed = def.attackSpeed;
            if (def.range > 0f)    { attackRange  = def.range; detectRange = def.range * 1.5f; }
            if (def.moveSpeed > 0f)  moveSpeed    = def.moveSpeed;
            damageType        = def.damageType;
            isAOE             = def.isAOE;
            aoeRadius         = def.aoeRadius;
            aoeFalloff        = def.aoeFalloff;
            hasActivable      = def.hasActivable;
            spellDuration     = def.spellDuration;
            spellCooldown     = def.spellCooldown;
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

        public void TakeDamageFrom(int amount, UnitStats attacker)
        {
            TakeDamage(amount);
            if (currentHealth <= 0 || attacker == null) return;
            GetComponent<UnitAttack>()?.TriggerCounterAttack(attacker);
        }

        public void TakeAOEDamage(int baseDamage, float distanceToCenter, float radius)
        {
            if (radius <= 0f) { TakeDamage(baseDamage); return; }
            float t = Mathf.Clamp01(distanceToCenter / radius);
            float mult = Mathf.Lerp(1f, 1f - aoeFalloff, t);
            ApplyDamage(ComputeReceivedDamage(Mathf.RoundToInt(baseDamage * mult)));
        }

        public void TakeAOEDamageFrom(int baseDamage, float distanceToCenter, float radius, UnitStats attacker)
        {
            TakeAOEDamage(baseDamage, distanceToCenter, radius);
            if (currentHealth <= 0 || attacker == null) return;
            GetComponent<UnitAttack>()?.TriggerCounterAttack(attacker);
        }

        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        private int ComputeReceivedDamage(int raw)
        {
            if (unitType == UnitType.Heavy && damageType != DamageType.Piercing)
                return Mathf.RoundToInt(raw * 0.7f);
            return raw;
        }

        private void ApplyDamage(int amount)
        {
            currentHealth = Mathf.Max(currentHealth - amount, 0);
            if (currentHealth <= 0) Die();
        }

        public void SetAsTarget(bool active)
        {
            if (active && targetHighlight == null) CreateTargetHighlight();
            if (targetHighlight != null) targetHighlight.SetActive(active);
        }

        private void CreateTargetHighlight()
        {
            if (targetHighlightMat == null)
            {
                Shader sh = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                targetHighlightMat = new Material(sh);
                targetHighlightMat.color = new Color(1f, 0.15f, 0.15f, 0.55f);
                targetHighlightMat.SetFloat("_Mode", 3);
                targetHighlightMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                targetHighlightMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                targetHighlightMat.SetInt("_ZWrite", 0);
                targetHighlightMat.EnableKeyword("_ALPHABLEND_ON");
                targetHighlightMat.renderQueue = 3000;
            }
            targetHighlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetHighlight.name = "TargetHighlight";
            targetHighlight.transform.SetParent(transform);
            targetHighlight.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            targetHighlight.transform.localScale    = new Vector3(0.9f, 0.008f, 0.9f);
            targetHighlight.GetComponent<Renderer>().sharedMaterial = targetHighlightMat;
            Destroy(targetHighlight.GetComponent<Collider>());
            targetHighlight.SetActive(false);
        }

        private void Die()
        {
            SetAsTarget(false);
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
