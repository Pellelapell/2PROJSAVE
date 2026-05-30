using UnityEngine;

namespace SupKonQuest
{
    [RequireComponent(typeof(UnitStats))]
    public class UnitAnimator : MonoBehaviour
    {
        [Tooltip("Laissez vide pour chercher automatiquement dans les enfants.")]
        [SerializeField] private Animator animator;

        private UnitMovement movement;
        private UnitAttack   attack;
        private UnitStats    stats;

        private static readonly int HashIsMoving    = Animator.StringToHash("IsMoving");
        private static readonly int HashIsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int HashIsDead      = Animator.StringToHash("IsDead");
        private static readonly int HashAttackSpeed = Animator.StringToHash("AttackSpeed");

        private bool  isDead;
        private float attackAnimTimer;

        private void Awake()
        {
            movement = GetComponent<UnitMovement>();
            attack   = GetComponent<UnitAttack>();
            stats    = GetComponent<UnitStats>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            Debug.Log($"[UnitAnimator] Awake sur {gameObject.name}");
        }

        private void Update()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator != null)
                    Debug.Log($"[UnitAnimator] Animator trouve sur {animator.gameObject.name}, controller = {animator.runtimeAnimatorController?.name}");
                else
                    return;
            }

            if (!isDead && stats != null && stats.currentHealth <= 0)
            {
                isDead = true;
                animator.speed = 1f;
                animator.SetBool(HashIsMoving,    false);
                animator.SetBool(HashIsAttacking, false);
                animator.SetBool(HashIsDead,      true);
                return;
            }
            if (isDead) return;

            if (attack != null && attack.IsAttacking && stats != null)
                attackAnimTimer = 1f / Mathf.Max(0.01f, stats.attackSpeed * stats.attackSpeedMultiplier);
            attackAnimTimer = Mathf.Max(0f, attackAnimTimer - Time.deltaTime);

            bool isAttacking = attackAnimTimer > 0f;
            bool isMoving    = !isAttacking && movement != null && movement.IsMoving;

            animator.SetBool(HashIsAttacking, isAttacking);
            animator.SetBool(HashIsMoving,    isMoving);
            animator.speed = (isMoving || isAttacking) ? 1f : 0f;

            if (stats != null)
            {
                float atkSpeed = Mathf.Max(0.01f, stats.attackSpeed * stats.attackSpeedMultiplier);
                animator.SetFloat(HashAttackSpeed, atkSpeed);
            }
        }
    }
}
