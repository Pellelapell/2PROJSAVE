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
        private GUIStyle btnStyle;

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
            GUI.Label(new Rect(cx, 6f, 140f, 22f), $"{L("hud_gold")} : {local.money}", titleStyle);
            cx += 150f;

            GUI.color = new Color(0.5f, 0.9f, 0.3f);
            GUI.Label(new Rect(cx, 6f, 140f, 22f), $"{L("hud_wood")} : {local.wood}", titleStyle);
            cx += 150f;

            GUI.color = local.playerColor;
            GUI.Label(new Rect(cx, 6f, 160f, 22f), $"{L("hud_camps")} : {local.ownedCamps.Count}", titleStyle);

            GUI.color = Color.white;

            // Bouton langue (coin haut-droit)
            if (LocalizationManager.Instance != null)
            {
                string langLabel = LocalizationManager.Instance.CurrentLanguage == "fr" ? "EN" : "FR";
                if (GUI.Button(new Rect(Screen.width - 52f, 4f, 44f, 24f), langLabel, titleStyle))
                {
                    string next = LocalizationManager.Instance.CurrentLanguage == "fr" ? "en" : "fr";
                    LocalizationManager.Instance.LoadLanguage(next);
                }
            }
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
            GUI.Label(new Rect(x, y, w, lineH), $"  {L("hud_region_bonus")}", noteStyle);
            GUI.color = Color.white;
            y += lineH;

            foreach (Region region in allRegions)
            {
                PlayerData owner = region.GetOwner();
                Color prev = GUI.color;
                GUI.color = owner != null ? owner.playerColor : new Color(0.6f, 0.6f, 0.6f);

                string label = owner != null
                    ? $"{region.GetDisplayName()} → {owner.playerName} (+{region.GetBonusGold()}g)"
                    : $"{region.GetDisplayName()} — {L("hud_region_free")}";

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
            GUI.Label(new Rect(x, y, w, 22f), $"{UnitDefaults.GetName(u.unitType)}  —  {(owner != null ? owner.playerName : L("hud_neutral"))}", titleStyle);
            GUI.color = prev;
            y += 24f;

            // Barre de vie
            float ratio = u.maxHealth > 0 ? (float)u.currentHealth / u.maxHealth : 1f;
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, w, 20f), Texture2D.whiteTexture);
            GUI.color = Color.Lerp(Color.red, Color.green, ratio);
            GUI.DrawTexture(new Rect(x, y, w * ratio, 20f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y + 2f, w, 20f), $"  {u.currentHealth} / {u.maxHealth}", rowStyle);
            y += 24f;

            // Stats
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y,       w * 0.5f, 18f), $"  {L("hud_damage")} : {u.attackDamage}", rowStyle);
            GUI.Label(new Rect(x + w*0.5f, y, w*0.5f, 18f), $"{L("hud_speed")} : {u.moveSpeed:F1}", rowStyle);
            y += 18f;
            GUI.Label(new Rect(x, y,       w * 0.5f, 18f), $"  {L("hud_range")} : {u.attackRange:F1}", rowStyle);
            GUI.Label(new Rect(x + w*0.5f, y, w*0.5f, 18f), $"{L("hud_atkspeed")} : {u.attackSpeed:F1}", rowStyle);
            y += 18f;
            string dmgType = L("damage_" + u.damageType.ToString());
            GUI.Label(new Rect(x, y, w, 18f), $"  {L("hud_type")} : {dmgType}{(u.isAOE ? "  [AOE]" : "")}", rowStyle);
            y += 18f;

            // Bonus région
            bool inRegion = RegionManager.Instance != null && RegionManager.Instance.IsInOwnedRegion(u.transform.position, u.ownerId);
            if (inRegion)
            {
                GUI.color = new Color(1f, 1f, 0.3f);
                GUI.Label(new Rect(x, y, w, 18f), $"  {L("hud_region_bonus")}", rowStyle);
                GUI.color = Color.white;
                y += 18f;
            }

            // Passagers si transport
            TransportShip ship = u.GetComponent<TransportShip>();
            if (ship != null)
            {
                GUI.Label(new Rect(x, y, w * 0.55f, 20f), $"  {ship.GetPassengerLabel()}", rowStyle);

                if (!ship.IsEmpty)
                {
                    Vector3 landPos = FindNearLand(u.transform.position, 5f);
                    bool landNear   = landPos != Vector3.zero;

                    if (landNear)
                    {
                        if (GUI.Button(new Rect(x + w * 0.55f, y, w * 0.45f, 20f), L("disembark"), btnStyle))
                            ship.DisembarkAll(landPos);
                    }
                    else
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.35f);
                        GUI.Label(new Rect(x + w * 0.55f, y, w * 0.45f, 20f), L("disembark"), rowStyle);
                        GUI.color = Color.white;
                    }
                }
                y += 22f;
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

        private static Vector3 FindNearLand(Vector3 from, float range)
        {
            Collider[] hits = Physics.OverlapSphere(from, range);
            foreach (Collider c in hits)
            {
                HexTile tile = c.GetComponentInParent<HexTile>();
                if (tile != null && tile.terrain == HexTerrain.Walkable)
                    return tile.transform.position;
            }
            return Vector3.zero;
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

            btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold
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
