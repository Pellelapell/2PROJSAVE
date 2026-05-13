using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    public class EconomyManager : MonoBehaviour
    {
        public float incomeInterval = 5f;
        public int moneyPerCamp = 10;

        private float timer;
        private GameManager gameManager;
        private RegionManager regionManager;

        private void Start()
        {
            gameManager = FindFirstObjectByType<GameManager>();
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
            foreach (PlayerData player in gameManager.players)
            {
                if (player.eliminated) continue;

                int campIncome = player.ownedCamps.Count * moneyPerCamp;
                int regionBonus = regionManager != null ? regionManager.GetRegionBonusGold(player) : 0;
                player.AddMoney(campIncome + regionBonus);

                SpawnRegionBonusUnits(player);
            }
        }

        private void SpawnRegionBonusUnits(PlayerData player)
        {
            if (regionManager == null) return;

            List<Region> ownedRegions = regionManager.GetRegionsOwnedBy(player);
            foreach (Region region in ownedRegions)
            {
                if (region.data == null || region.data.bonusUnitCount <= 0) continue;

                Camp spawnCamp = regionManager.GetBonusSpawnCamp(region, player);
                if (spawnCamp == null) continue;

                CampProduction prod = spawnCamp.GetComponent<CampProduction>();
                if (prod == null) continue;

                for (int i = 0; i < region.data.bonusUnitCount; i++)
                    prod.SpawnUnitInstant(region.data.bonusUnitType);
            }
        }
    }
}
