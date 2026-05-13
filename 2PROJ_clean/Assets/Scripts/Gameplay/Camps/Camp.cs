using UnityEngine;

namespace SupKonQuest
{
    public class Camp : MonoBehaviour
    {
        [Header("Camp")]
        public CampType campType;
        public bool isNeutral = true;

        [Header("Owner")]
        public PlayerData owner;

        [Header("Spawn")]
        public Transform spawnPoint;

        [Header("Health")]
        public int maxHP = 300;
        public int currentHP = 300;

        // attacker est l'unité qui inflige le coup fatal (peut être null si turret ou AOE indirect)
        public void TakeDamage(int amount, UnitStats attacker)
        {
            currentHP = Mathf.Max(0, currentHP - amount);

            if (currentHP <= 0)
            {
                // Mort mutuelle : si l'attaquant est mort en même temps → camp neutre
                bool mutualDeath = attacker != null && attacker.currentHealth <= 0;
                PlayerData newOwner = mutualDeath ? null : (attacker != null ? GameManager.Instance?.GetPlayerById(attacker.ownerId) : null);
                SetOwner(newOwner);
                currentHP = maxHP;
            }
        }

        // Surcharge pour les dégâts de la tourelle (pas d'attaquant → jamais de capture par turret)
        public void TakeDamageFromTurret(int amount)
        {
            currentHP = Mathf.Max(0, currentHP - amount);
            if (currentHP <= 0)
            {
                SetOwner(null);
                currentHP = maxHP;
            }
        }

        public void SetOwner(PlayerData newOwner)
        {
            if (owner == newOwner) return;

            PlayerData previousOwner = owner;

            if (owner != null)
                owner.ownedCamps.Remove(this);

            owner = newOwner;
            isNeutral = (newOwner == null);

            if (owner != null && !owner.ownedCamps.Contains(this))
                owner.ownedCamps.Add(this);

            UpdateCampVisual();
            GameManager.Instance?.NotifyCampCaptured(this, previousOwner);
        }

        private void UpdateCampVisual()
        {
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend == null) return;
            rend.material.color = owner == null ? Color.gray : owner.playerColor;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
}
