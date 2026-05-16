using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    public class PlayerData : MonoBehaviour
    {
        [Header("Identity")]
        public int playerId;
        public string playerName = "Player";
        public Race race;
        public Color playerColor = Color.white;

        [Header("Economy")]
        public int money = 150;
        public int wood  = 30;

        [Header("State")]
        public bool eliminated = false;
        public bool isAI = false;

        [HideInInspector] public List<Camp> ownedCamps = new List<Camp>();

        private void Awake()
        {
            if (playerColor == Color.white)
                playerColor = race switch
                {
                    Race.Human => Color.blue,
                    Race.Elf => Color.green,
                    Race.Demon => Color.red,
                    _ => Color.white
                };
        }

        public void AddMoney(int amount) => money += amount;
        public void AddWood(int amount)  => wood  += amount;

        public bool SpendMoney(int amount)
        {
            if (money < amount) return false;
            money -= amount;
            return true;
        }

        public bool SpendResources(int goldCost, int woodCost)
        {
            if (money < goldCost || wood < woodCost) return false;
            money -= goldCost;
            wood  -= woodCost;
            return true;
        }

        public bool CanAfford(int goldCost, int woodCost) => money >= goldCost && wood >= woodCost;
    }
}
