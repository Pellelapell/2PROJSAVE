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
        private GUIStyle endStyle;

        private bool showEndScreen;
        private bool localPlayerWon;

        private const float PanelWidth  = 240f;
        private const float RowHeight   = 62f;
        private const float HeaderHeight = 28f;
        private const float Margin      = 10f;

        private void Start()
        {
            gameManager    = GameManager.Instance;
            economyManager = FindFirstObjectByType<EconomyManager>();
            regionManager  = RegionManager.Instance;

            if (gameManager != null)
                gameManager.OnGameOver += HandleGameOver;
        }

        private void OnDestroy()
        {
            if (gameManager != null)
                gameManager.OnGameOver -= HandleGameOver;
        }

        private void HandleGameOver(PlayerData winner)
        {
            showEndScreen   = true;
            localPlayerWon  = winner.playerId == gameManager.localPlayerId;
        }

        private void OnGUI()
        {
            if (gameManager == null) return;
            InitStyles();

            DrawTopBar();
            DrawLeaderboard();
            DrawUnitStats();

            if (showEndScreen)
                DrawEndScreen();
        }

        // ── Barre de ressources (local player) ───────────────────────

        private void DrawTopBar()
        {
            PlayerData local = null;
            foreach (PlayerData p in gameManager.players)
                if (!p.isAI) { local = p; break; }
            if (local == null) return;

            float barH = 32f;
            float barW = Screen.width * 0.5f;
            float barX = (Screen.width - barW) * 0.5f;

            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(barX - 8, 0, barW + 16, barH + 4), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float cx = barX + 10f;

            // Or
            GUI.color = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(cx, 6f, 140f, 22f), $"Or : {local.money}", titleStyle);
            cx += 150f;

            // Bois
            GUI.color = new Color(0.5f, 0.9f, 0.3f);
            GUI.Label(new Rect(cx, 6f, 140f, 22f), $"Bois : {local.wood}", titleStyle);
            cx += 150f;

            // Camps
            GUI.color = local.playerColor;
            GUI.Label(new Rect(cx, 6f, 160f, 22f), $"Camps : {local.ownedCamps.Count}", titleStyle);

            GUI.color = Color.white;
        }

        // ── Classement ───────────────────────────────────────────────

        private void DrawLeaderboard()
        {
            float rowH    = 40f;
            float panelHeight = HeaderHeight + gameManager.players.Length * rowH + Margin;
            float x = Screen.width - PanelWidth - Margin;
            float y = Margin + 40f; // décalé sous la top bar

            GUI.Box(new Rect(x - 5, y - 5, PanelWidth + 10, panelHeight), "", boxStyle);
            GUI.Label(new Rect(x, y, PanelWidth, HeaderHeight), L("hud_title"), titleStyle);
            y += HeaderHeight;

            foreach (PlayerData player in gameManager.players)
            {
                Color prev = GUI.color;
                GUI.color = player.eliminated ? new Color(0.5f, 0.5f, 0.5f) : player.playerColor;

                string status = player.eliminated ? L("hud_eliminated")
                              : player.isAI       ? L("hud_ai")
                              :                     L("hud_you");

                GUI.Label(new Rect(x, y,       PanelWidth, 22f), $"{player.playerName}  {status}", titleStyle);
                GUI.Label(new Rect(x, y + 20f, PanelWidth, 18f), $"  Camps : {player.ownedCamps.Count}", rowStyle);

                GUI.color = prev;
                y += rowH;
            }

            if (regionManager != null)
                DrawRegionPanel();
        }

        // ── Régions ──────────────────────────────────────────────────

        private void DrawRegionPanel()
        {
            Region[] allRegions = regionManager.GetAllRegions();
            if (allRegions == null || allRegions.Length == 0) return;

            float x     = Margin;
            float y     = Margin;
            float w     = 220f;
            float lineH = 20f;
            float panelH = lineH * (allRegions.Length + 2) + 10f;

            GUI.Box(new Rect(x - 5, y - 5, w + 10, panelH), "", boxStyle);
            GUI.Label(new Rect(x, y, w, lineH), L("hud_regions"), titleStyle);
            y += lineH;

            // Note sur le bonus de combat
            GUIStyle noteStyle = new GUIStyle(rowStyle) { fontSize = 10 };
            GUI.color = new Color(1f, 1f, 0.5f);
            GUI.Label(new Rect(x, y, w, lineH), "  ★ Région alliée : +20% dégâts", noteStyle);
            GUI.color = Color.white;
            y += lineH;

            foreach (Region region in allRegions)
            {
                PlayerData owner = region.GetOwner();
                Color prev = GUI.color;
                GUI.color = owner != null ? owner.playerColor : new Color(0.6f, 0.6f, 0.6f);

                string label = owner != null
                    ? $"{region.GetDisplayName()} → {owner.playerName} (+{region.data?.bonusGold ?? 0}g)"
                    : $"{region.GetDisplayName()} — libre";

                GUI.Label(new Rect(x, y, w, lineH), label, regionStyle);
                GUI.color = prev;
                y += lineH;
            }
        }

        // ── Stats unité sélectionnée ─────────────────────────────────

        private void DrawUnitStats()
        {
            UnitStats u = InputManager.Instance?.SelectedUnitStats;
            if (u == null) return;

            const float w = 260f;
            const float h = 170f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - h - 10f;

            GUI.Box(new Rect(x - 5, y - 5, w + 10, h + 10), "", boxStyle);

            // Titre : type + propriétaire
            PlayerData owner = GameManager.Instance?.GetPlayerById(u.ownerId);
            Color prev = GUI.color;
            GUI.color = owner != null ? owner.playerColor : Color.white;
            GUI.Label(new Rect(x, y, w, 22f), $"{u.unitType}  —  {(owner != null ? owner.playerName : "Neutre")}", titleStyle);
            GUI.color = prev;
            y += 24f;

            // Barre de vie
            float ratio = u.maxHealth > 0 ? (float)u.currentHealth / u.maxHealth : 1f;
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, w, 12f), Texture2D.whiteTexture);
            GUI.color = Color.Lerp(Color.red, Color.green, ratio);
            GUI.DrawTexture(new Rect(x, y, w * ratio, 12f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, w, 12f), $"  {u.currentHealth} / {u.maxHealth}", rowStyle);
            y += 16f;

            // Stats
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y,       w * 0.5f, 18f), $"  Dégâts : {u.attackDamage}", rowStyle);
            GUI.Label(new Rect(x + w*0.5f, y, w*0.5f, 18f), $"Vitesse : {u.moveSpeed:F1}", rowStyle);
            y += 18f;
            GUI.Label(new Rect(x, y,       w * 0.5f, 18f), $"  Portée atk : {u.attackRange:F1}", rowStyle);
            GUI.Label(new Rect(x + w*0.5f, y, w*0.5f, 18f), $"Atk/s : {u.attackSpeed:F1}", rowStyle);
            y += 18f;
            GUI.Label(new Rect(x, y, w, 18f), $"  Type : {u.damageType}{(u.isAOE ? "  [AOE]" : "")}", rowStyle);
            y += 18f;

            // Bonus région
            bool inRegion = RegionManager.Instance != null && RegionManager.Instance.IsInOwnedRegion(u.transform.position, u.ownerId);
            if (inRegion)
            {
                GUI.color = new Color(1f, 1f, 0.3f);
                GUI.Label(new Rect(x, y, w, 18f), "  ★ Région alliée : +20% dégâts", rowStyle);
                GUI.color = Color.white;
                y += 18f;
            }

            // Passagers si transport
            TransportShip ship = u.GetComponent<TransportShip>();
            if (ship != null)
            {
                GUI.Label(new Rect(x, y, w, 18f), $"  {ship.GetPassengerLabel()}  |  [E] Débarquer", rowStyle);
            }
        }

        // ── Écran de fin ─────────────────────────────────────────────

        private void DrawEndScreen()
        {
            // Fond semi-transparent plein écran
            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            float w = 500f, h = 160f;
            float x = (Screen.width  - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            string msg = localPlayerWon ? L("victory") : L("defeat");
            endStyle.normal.textColor = localPlayerWon ? new Color(1f, 0.9f, 0.1f) : new Color(1f, 0.25f, 0.25f);

            GUI.Label(new Rect(x, y, w, h), msg, endStyle);
        }

        // ── Styles ───────────────────────────────────────────────────

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
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };

            rowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal   = { textColor = Color.white }
            };

            regionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal   = { textColor = Color.white }
            };

            endStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 64,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
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
