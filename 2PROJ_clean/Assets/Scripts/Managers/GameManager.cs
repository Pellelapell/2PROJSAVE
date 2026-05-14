using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    [DefaultExecutionOrder(-5)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Players")]
        public PlayerData[] players;
        public int localPlayerId = 1;

        [Header("Camps")]
        public Camp[] camps;
        public int campsPerPlayer = 3;

        [Header("State")]
        public bool gameStarted = false;
        public bool gameOver = false;

        public event System.Action<PlayerData> OnPlayerEliminated;
        public event System.Action<PlayerData> OnGameOver;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (camps == null || camps.Length == 0)
                camps = FindObjectsByType<Camp>(FindObjectsSortMode.None);

            AssignCampsRandomly();
            gameStarted = true;
        }

        private void AssignCampsRandomly()
        {
            // Seuls les camps normaux sont distribuables aux joueurs
            List<Camp> pool = new List<Camp>();
            foreach (Camp c in camps)
                if (c.campType == CampType.Normal) pool.Add(c);

            // Mélange aléatoire
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            int idx = 0;
            foreach (PlayerData player in players)
            {
                player.ownedCamps.Clear();
                for (int i = 0; i < campsPerPlayer && idx < pool.Count; i++, idx++)
                    pool[idx].SetOwner(player);
            }
            // Les camps restants et les NeutralSpecial restent neutres
        }

        public void NotifyCampCaptured(Camp camp, PlayerData previousOwner)
        {
            if (!gameStarted || gameOver) return;

            if (previousOwner != null && !previousOwner.eliminated && previousOwner.ownedCamps.Count == 0)
                EliminatePlayer(previousOwner);

            CheckWinCondition();
        }

        private void EliminatePlayer(PlayerData player)
        {
            player.eliminated = true;

            UnitStats[] allUnits = FindObjectsByType<UnitStats>(FindObjectsSortMode.None);
            foreach (UnitStats unit in allUnits)
                if (unit.ownerId == player.playerId)
                    unit.ownerId = 0;

            OnPlayerEliminated?.Invoke(player);
            Debug.Log($"Player {player.playerName} eliminated!");
        }

        private void CheckWinCondition()
        {
            List<PlayerData> alive = new List<PlayerData>();
            foreach (PlayerData p in players)
                if (!p.eliminated) alive.Add(p);

            if (alive.Count == 1)
            {
                gameOver = true;
                OnGameOver?.Invoke(alive[0]);
            }
        }

        public PlayerData GetPlayerById(int id)
        {
            foreach (PlayerData player in players)
                if (player.playerId == id) return player;
            return null;
        }
    }
}
