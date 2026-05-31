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
        [System.NonSerialized] public PlayerData[] activePlayers;
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
            var active = new System.Collections.Generic.List<PlayerData>();
            foreach (PlayerData p in players)
                if (p != null && p.gameObject.activeInHierarchy) active.Add(p);
            activePlayers = active.ToArray();

            AssignCampsByCorner();
            gameStarted = true;
        }

        private void AssignCampsByCorner()
        {
            List<Camp>[] corners = HexGridGenerator.CornerCamps;

            if (corners == null)
            {
                Debug.LogWarning("[GameManager] CornerCamps null — aucun camp assigné.");
                return;
            }

            for (int i = 0; i < activePlayers.Length; i++)
            {
                activePlayers[i].ownedCamps.Clear();

                if (i >= corners.Length || corners[i] == null) continue;

                foreach (Camp camp in corners[i])
                    camp.SetOwner(activePlayers[i]);

                Debug.Log($"[GameManager] {activePlayers[i].playerName} → {corners[i].Count} camp(s) coin {i}.");
            }
        }

        private float eliminationCheckTimer;

        private void Update()
        {
            if (!gameStarted || gameOver) return;
            eliminationCheckTimer -= Time.deltaTime;
            if (eliminationCheckTimer > 0f) return;
            eliminationCheckTimer = 3f;

            foreach (PlayerData p in activePlayers)
            {
                if (p.eliminated) continue;
                if (p.ownedCamps.Count == 0 && CountUnits(p) == 0)
                    EliminatePlayer(p);
            }
            CheckWinCondition();
        }

        public void NotifyCampCaptured(Camp camp, PlayerData previousOwner)
        {
            if (!gameStarted || gameOver) return;
            CheckWinCondition();
        }

        private int CountUnits(PlayerData p)
        {
            int n = 0;
            foreach (UnitStats us in FindObjectsByType<UnitStats>(FindObjectsSortMode.None))
                if (us.ownerId == p.playerId && us.gameObject.activeInHierarchy && us.currentHealth > 0)
                    n++;
            return n;
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
            foreach (PlayerData p in activePlayers)
                if (!p.eliminated) alive.Add(p);

            if (alive.Count == 1)
            {
                gameOver = true;
                OnGameOver?.Invoke(alive[0]);
            }
        }

        public PlayerData GetPlayerById(int id)
        {
            foreach (PlayerData player in activePlayers)
                if (player.playerId == id) return player;
            return null;
        }
    }
}
