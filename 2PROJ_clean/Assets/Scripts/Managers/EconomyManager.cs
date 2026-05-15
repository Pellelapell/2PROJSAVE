using UnityEngine;

namespace SupKonQuest
{
    public class EconomyManager : MonoBehaviour
    {
        public float incomeInterval = 8f;
        public int moneyPerCamp = 8;      // or passif réduit

        private float timer;
        private GameManager gameManager;
        private RegionManager regionManager;

        private void Start()
        {
            gameManager   = FindFirstObjectByType<GameManager>();
            regionManager = RegionManager.Instance;
        }

        private void Update()
        {
            if (gameManager == null) return;
            timer += Time.deltaTime;
            if (timer >= incomeInterval)
            {
                timer = 0f;
                GiveIncome();
            }
        }

        private void GiveIncome()
        {
            Sawmill[] sawmills = FindObjectsByType<Sawmill>(FindObjectsSortMode.None);

            foreach (PlayerData player in gameManager.players)
            {
                if (player.eliminated) continue;

                // Or : camps + bonus région (réduit)
                int campIncome  = player.ownedCamps.Count * moneyPerCamp;
                int regionBonus = regionManager != null ? regionManager.GetRegionBonusGold(player) : 0;
                player.AddMoney(campIncome + regionBonus);

                // Bois : scieries
                foreach (Sawmill saw in sawmills)
                    if (saw.owner == player) player.AddWood(saw.woodPerTick);
            }
        }
    }
}
