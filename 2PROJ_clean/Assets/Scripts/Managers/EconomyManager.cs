using UnityEngine;

namespace SupKonQuest
{
    public class EconomyManager : MonoBehaviour
    {
        public float incomeInterval = 5f;
        public int moneyPerCamp = 10;

        private float timer;
        private GameManager gameManager;

        private void Start()
        {
            gameManager = FindFirstObjectByType<GameManager>();
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
                int income = player.ownedCamps.Count * moneyPerCamp;
                player.AddMoney(income);
                Debug.Log($"Player {player.playerId} receives {income} gold. Total = {player.money}");
            }
        }
    }
}