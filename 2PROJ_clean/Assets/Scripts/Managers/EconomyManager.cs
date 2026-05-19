using UnityEngine;

namespace SupKonQuest
{
    public class EconomyManager : MonoBehaviour
    {
        public float incomeInterval = 8f;
        public int   baseGoldPerTick = 3;   // or passif de base (sans camp)
        public int   moneyPerCamp    = 5;   // or par camp possédé
        public int   baseWoodPerTick = 3;   // bois passif de base (sans scierie)

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

            foreach (PlayerData player in gameManager.activePlayers)
            {
                if (player.eliminated) continue;

                // Or : base + camps + bonus région
                int campIncome  = player.ownedCamps.Count * moneyPerCamp;
                int regionBonus = regionManager != null ? regionManager.GetRegionBonusGold(player) : 0;
                player.AddMoney(baseGoldPerTick + campIncome + regionBonus);

                // Bois : base + scieries
                int woodIncome = baseWoodPerTick;
                foreach (Sawmill saw in sawmills)
                    if (saw.owner == player) woodIncome += saw.woodPerTick;
                player.AddWood(woodIncome);
            }
        }
    }
}
