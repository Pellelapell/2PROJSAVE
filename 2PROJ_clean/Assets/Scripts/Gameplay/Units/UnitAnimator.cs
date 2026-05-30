using UnityEngine;

namespace SupKonQuest
{
    // À ajouter sur le même GameObject que UnitMovement / UnitAttack / UnitStats.
    // Requiert un Animator sur le modèle 3D enfant (ou sur ce GameObject).
    // Paramètres attendus dans l'Animator Controller :
    //   bool  "IsMoving"   – marche / idle
    //   bool  "IsAttacking"– frappe
    //   bool  "IsDead"     – mort
    [RequireComponent(typeof(UnitStats))]
    public class UnitAnimator : MonoBehaviour
    {
        [Tooltip("Laissez vide pour chercher automatiquement dans les enfants.")]
        [SerializeField] private Animator animator;

        private UnitMovement movement;
        private UnitAttack   attack;
        private UnitStats    stats;

        // IDs mis en cache pour éviter les lookups string à chaque frame
        private static readonly int HashIsMoving    = Animator.StringToHash("IsMoving");
        private static readonly int HashIsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int HashIsDead      = Animator.StringToHash("IsDead");
        private static readonly int HashAttackSpeed = Animator.StringToHash("AttackSpeed");

        private bool isDead;

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
            // on cherche l'animator en lazy car le modele est instancie apres le Awake
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator != null)
                    Debug.Log($"[UnitAnimator] Animator trouve sur {animator.gameObject.name}, controller = {animator.runtimeAnimatorController?.name}");
                else
                    return;
            }

            // Mort — état terminal, on joue une fois et on ne touche plus à rien
            if (!isDead && stats != null && stats.currentHealth <= 0)
            {
                isDead = true;
                animator.SetBool(HashIsDead, true);
                return;
            }
            if (isDead) return;

            // Déplacement
            bool isMoving = movement != null && movement.IsMoving;
            animator.SetBool(HashIsMoving, isMoving);

            // Attaque
            bool isAttacking = attack != null && attack.IsAttacking;
            animator.SetBool(HashIsAttacking, isAttacking);

            // Vitesse d'attaque — utile pour synchroniser la durée du clip
            if (stats != null)
            {
                float atkSpeed = Mathf.Max(0.01f, stats.attackSpeed * stats.attackSpeedMultiplier);
                animator.SetFloat(HashAttackSpeed, atkSpeed);
            }
        }
    }
}
