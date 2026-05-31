using UnityEngine;

namespace SupKonQuest
{
    public class EconomyManager : MonoBehaviour
    {
        public float incomeInterval = 8f;
        public int   baseGoldPerTick = 3;
        public int   moneyPerCamp    = 5;
        public int   baseWoodPerTick = 3;

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
            if (gameManager.activePlayers == null) return;

            Sawmill[] sawmills = FindObjectsByType<Sawmill>(FindObjectsSortMode.None);

            foreach (PlayerData player in gameManager.activePlayers)
            {
                if (player.eliminated) continue;

                int campIncome  = player.ownedCamps.Count * moneyPerCamp;
                int regionBonus = regionManager != null ? regionManager.GetRegionBonusGold(player) : 0;
                player.AddMoney(baseGoldPerTick + campIncome + regionBonus);

                int woodIncome = baseWoodPerTick;
                foreach (Sawmill saw in sawmills)
                    if (saw.owner == player) woodIncome += saw.woodPerTick;
                player.AddWood(woodIncome);
            }
        }
    }
}
