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
            // Ne garder que les joueurs dont le GameObject est actif (set par GameBootstrap)
            System.Collections.Generic.List<PlayerData> active = new System.Collections.Generic.List<PlayerData>();
            foreach (PlayerData p in players)
                if (p != null && p.gameObject.activeInHierarchy) active.Add(p);
            players = active.ToArray();

            AssignCampsByCorner();
            gameStarted = true;
        }

        // Chaque joueur reçoit les camps du coin qui lui correspond (coin 0 → joueur 0, etc.)
        private void AssignCampsByCorner()
        {
            List<Camp>[] corners = HexGridGenerator.CornerCamps;

            if (corners == null)
            {
                Debug.LogWarning("[GameManager] CornerCamps null — aucun camp assigné.");
                return;
            }

            for (int i = 0; i < players.Length; i++)
            {
                players[i].ownedCamps.Clear();

                if (i >= corners.Length || corners[i] == null) continue;

                foreach (Camp camp in corners[i])
                    camp.SetOwner(players[i]);

                Debug.Log($"[GameManager] {players[i].playerName} → {corners[i].Count} camp(s) coin {i}.");
            }
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
