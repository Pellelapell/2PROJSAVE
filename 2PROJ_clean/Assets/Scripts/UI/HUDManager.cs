using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    public class HUDManager : MonoBehaviour
    {
        private GameManager gameManager;
        private EconomyManager economyManager;
        private RegionManager regionManager;

        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle rowStyle;
        private GUIStyle regionStyle;

        private const float PanelWidth = 240f;
        private const float RowHeight = 62f;
        private const float HeaderHeight = 28f;
        private const float Margin = 10f;

        private void Start()
        {
            gameManager = GameManager.Instance;
            economyManager = FindFirstObjectByType<EconomyManager>();
            regionManager = RegionManager.Instance;
        }

        private void OnGUI()
        {
            if (gameManager == null) return;
            InitStyles();

            DrawLeaderboard();
        }

        private void DrawLeaderboard()
        {
            float panelHeight = HeaderHeight + gameManager.players.Length * RowHeight + Margin;
            float x = Screen.width - PanelWidth - Margin;
            float y = Margin;

            GUI.Box(new Rect(x - 5, y - 5, PanelWidth + 10, panelHeight), "", boxStyle);
            GUI.Label(new Rect(x, y, PanelWidth, HeaderHeight), L("hud_title"), titleStyle);
            y += HeaderHeight;

            foreach (PlayerData player in gameManager.players)
            {
                Color prev = GUI.color;
                GUI.color = player.eliminated ? new Color(0.5f, 0.5f, 0.5f) : player.playerColor;

                int campIncome = player.ownedCamps.Count * (economyManager != null ? economyManager.moneyPerCamp : 10);
                int regionBonus = regionManager != null ? regionManager.GetRegionBonusGold(player) : 0;
                int totalIncome = campIncome + regionBonus;

                string status = player.eliminated ? L("hud_eliminated")
                              : player.isAI ? L("hud_ai")
                              : L("hud_you");

                GUI.Label(new Rect(x, y, PanelWidth, 22f), $"{player.playerName} {status}", titleStyle);
                GUI.Label(new Rect(x, y + 22f, PanelWidth, 18f), $"  {L("hud_camps")}: {player.ownedCamps.Count}  {L("hud_gold")}: {player.money}g", rowStyle);

                string incomeStr = regionBonus > 0
                    ? $"  +{campIncome}g (+{regionBonus} région)"
                    : $"  +{campIncome}g";
                GUI.Label(new Rect(x, y + 40f, PanelWidth, 18f), incomeStr, rowStyle);

                GUI.color = prev;
                y += RowHeight;
            }

            // Afficher les régions complètes si RegionManager disponible
            if (regionManager != null)
                DrawRegionPanel();
        }

        private void DrawRegionPanel()
        {
            Region[] allRegions = regionManager.GetAllRegions();
            if (allRegions == null || allRegions.Length == 0) return;

            float x = Margin;
            float y = Margin;
            float w = 200f;
            float lineH = 20f;
            float panelH = lineH * (allRegions.Length + 1) + 10f;

            GUI.Box(new Rect(x - 5, y - 5, w + 10, panelH), "", boxStyle);
            GUI.Label(new Rect(x, y, w, lineH), L("hud_regions"), titleStyle);
            y += lineH;

            foreach (Region region in allRegions)
            {
                PlayerData owner = region.GetOwner();
                Color prev = GUI.color;
                GUI.color = owner != null ? owner.playerColor : new Color(0.6f, 0.6f, 0.6f);

                string label = owner != null
                    ? $"{region.GetDisplayName()} → {owner.playerName} (+{region.data.bonusGold}g)"
                    : $"{region.GetDisplayName()} — libre";

                GUI.Label(new Rect(x, y, w, lineH), label, regionStyle);
                GUI.color = prev;
                y += lineH;
            }
        }

        private static string L(string key) => LocalizationManager.Get(key);

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

            regionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
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
