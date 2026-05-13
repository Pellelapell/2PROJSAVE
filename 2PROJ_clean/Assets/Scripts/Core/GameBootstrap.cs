using UnityEngine;

namespace SupKonQuest
{
    /// <summary>
    /// Reads PlayerPrefs set by MainMenu and configures the scene before other scripts Start().
    /// Attach this to a GameObject in the game scene with a higher execution order than HexGridGenerator.
    /// </summary>
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
