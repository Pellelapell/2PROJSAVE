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

        public void TakeDamage(int amount, int attackerId)
        {
            currentHP = Mathf.Max(0, currentHP - amount);

            if (currentHP <= 0)
            {
                PlayerData attacker = GameManager.Instance?.GetPlayerById(attackerId);
                SetOwner(attacker);
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
