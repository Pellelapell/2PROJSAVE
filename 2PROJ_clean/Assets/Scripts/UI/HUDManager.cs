using UnityEngine;

namespace SupKonQuest
{
    public class HUDManager : MonoBehaviour
    {
        private GameManager gameManager;
        private EconomyManager economyManager;

        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle rowStyle;

        private const float PanelWidth = 220f;
        private const float RowHeight = 55f;
        private const float HeaderHeight = 28f;
        private const float Margin = 10f;

        private void Start()
        {
            gameManager = GameManager.Instance;
            economyManager = FindFirstObjectByType<EconomyManager>();
        }

        private void OnGUI()
        {
            if (gameManager == null) return;
            InitStyles();

            float panelHeight = HeaderHeight + gameManager.players.Length * RowHeight + Margin;
            float x = Screen.width - PanelWidth - Margin;
            float y = Margin;

            GUI.Box(new Rect(x - 5, y - 5, PanelWidth + 10, panelHeight), "", boxStyle);
            GUI.Label(new Rect(x, y, PanelWidth, HeaderHeight), "Classement", titleStyle);
            y += HeaderHeight;

            foreach (PlayerData player in gameManager.players)
            {
                Color prev = GUI.color;
                GUI.color = player.eliminated ? new Color(0.5f, 0.5f, 0.5f) : player.playerColor;

                int incomePerTick = player.ownedCamps.Count * (economyManager != null ? economyManager.moneyPerCamp : 10);
                string status = player.eliminated ? " [éliminé]" : (player.isAI ? " [IA]" : " [vous]");

                GUI.Label(new Rect(x, y, PanelWidth, 22f), $"{player.playerName}{status}", titleStyle);
                GUI.Label(new Rect(x, y + 22f, PanelWidth, 18f), $"  Camps: {player.ownedCamps.Count}   Or: {player.money}g (+{incomePerTick})", rowStyle);

                GUI.color = prev;
                y += RowHeight;
            }
        }

        private void InitStyles()
        {
            if (boxStyle != null) return;

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(1, 1, new Color(0f, 0f, 0f, 0.6f)) }
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            rowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            Texture2D tex = new Texture2D(w, h);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }
    }
}
