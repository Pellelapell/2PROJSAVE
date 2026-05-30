using System;
using UnityEngine;

namespace SupKonQuest
{
    [DefaultExecutionOrder(-20)]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("References")]
        public HexGridGenerator hexGridGenerator;
        public GameManager gameManager;

        [Header("AI Players (assign all possible AI PlayerData)")]
        public PlayerData[] aiPlayers;
        public AIController[] aiControllers;

        private void Awake()
        {
            ApplyMapType();
            ApplyAIConfig();
            ApplyPlayerRace();
        }

        private void ApplyMapType()
        {
            if (hexGridGenerator == null)
                hexGridGenerator = FindFirstObjectByType<HexGridGenerator>();
            if (hexGridGenerator == null) return;

            int mapIdx = PlayerPrefs.GetInt("MapType", 0);
            hexGridGenerator.mapType = mapIdx switch
            {
                1 => MapType.FrozenPeaks,
                2 => MapType.Island,
                _ => MapType.Classic
            };
        }

        private static readonly Race[] MenuRaceOrder = { Race.Human, Race.Elf, Race.Demon };

        private void ApplyPlayerRace()
        {
            int idx = PlayerPrefs.GetInt("PlayerRace", 0);
            Race humanRace = MenuRaceOrder[Mathf.Clamp(idx, 0, MenuRaceOrder.Length - 1)];

            PlayerData[] allPlayers = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);

            foreach (PlayerData p in allPlayers)
            {
                if (p.isAI) continue;
                p.race       = humanRace;
                p.playerColor = RaceColor(humanRace);
                break;
            }

            int aiOffset = 1;
            foreach (PlayerData p in allPlayers)
            {
                if (!p.isAI) continue;
                Race aiRace   = MenuRaceOrder[(Array.IndexOf(MenuRaceOrder, humanRace) + aiOffset) % MenuRaceOrder.Length];
                p.race        = aiRace;
                p.playerColor = RaceColor(aiRace);
                aiOffset++;
            }
        }

        private static Color RaceColor(Race race) => race switch
        {
            Race.Human => new Color(0.3f, 0.5f, 1.0f),
            Race.Elf   => new Color(0.2f, 0.85f, 0.3f),
            Race.Demon => new Color(1.0f, 0.25f, 0.25f),
            _          => Color.white
        };

        private void ApplyAIConfig()
        {
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();

            int aiCount = PlayerPrefs.GetInt("AICount", 1);
            int diff = PlayerPrefs.GetInt("AIDifficulty", 0);

            if (aiPlayers != null)
            {
                for (int i = 0; i < aiPlayers.Length; i++)
                {
                    if (aiPlayers[i] == null) continue;
                    aiPlayers[i].gameObject.SetActive(i < aiCount);
                }
            }

            if (aiControllers != null)
            {
                for (int i = 0; i < aiControllers.Length; i++)
                {
                    if (aiControllers[i] == null) continue;
                    bool active = i < aiCount;
                    aiControllers[i].difficultyLevel = diff;
                    aiControllers[i].gameObject.SetActive(active);
                }
            }
        }
    }
}
