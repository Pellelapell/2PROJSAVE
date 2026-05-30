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
        private GUIStyle sunkStyle;

        private bool showEndScreen;
        private bool localPlayerWon;

        private List<string> sunkPassengers;
        private float sunkNotifTimer;
        private const float SunkNotifDuration = 6f;

        private int cachedPop;
        private int cachedPopCap;
        private float popRefreshTimer;

        private const float PanelWidth   = 240f;
        private const float RowHeight    = 62f;
        private const float HeaderHeight = 28f;
        private const float Margin       = 10f;

        private void Start()
        {
            gameManager    = GameManager.Instance;
            economyManager = FindFirstObjectByType<EconomyManager>();
            regionManager  = RegionManager.Instance;

            if (gameManager != null)
                gameManager.OnGameOver += HandleGameOver;

            TransportShip.OnShipSunkWithPassengers += HandleShipSunk;
        }

        private void OnDestroy()
        {
            if (gameManager != null)
                gameManager.OnGameOver -= HandleGameOver;

            TransportShip.OnShipSunkWithPassengers -= HandleShipSunk;
        }

        private void Update()
        {
            if (sunkNotifTimer > 0f)
                sunkNotifTimer -= Time.deltaTime;

            popRefreshTimer -= Time.deltaTime;
            if (popRefreshTimer <= 0f)
            {
                popRefreshTimer = 1f;
                RefreshPopCount();
            }
        }

        private void RefreshPopCount()
        {
            if (gameManager == null || gameManager.activePlayers == null) return;
            PlayerData local = null;
            foreach (PlayerData p in gameManager.activePlayers)
                if (!p.isAI) { local = p; break; }
            if (local == null) return;

            cachedPopCap = local.ownedCamps.Count * 10;
            int count = 0;
            UnitStats[] all = FindObjectsByType<UnitStats>(FindObjectsSortMode.None);
            foreach (UnitStats us in all)
                if (us.ownerId == local.playerId && us.gameObject.activeInHierarchy) count++;
            cachedPop = count;
        }

        private void HandleGameOver(PlayerData winner)
        {
            showEndScreen  = true;
            localPlayerWon = winner.playerId == gameManager.localPlayerId;
        }

        private void HandleShipSunk(List<string> unitNames)
        {
            sunkPassengers  = unitNames;
            sunkNotifTimer  = SunkNotifDuration;
        }

        private void OnGUI()
        {
            if (gameManager == null) return;
            InitStyles();

            DrawTopBar();
            DrawLeaderboard();
            DrawUnitStats();
            DrawBuildingInfo();

            if (sunkNotifTimer > 0f && sunkPassengers != null && sunkPassengers.Count > 0)
                DrawSunkNotification();

            if (showEndScreen)
                DrawEndScreen();
        }

        private void DrawTopBar()
        {
            if (gameManager.activePlayers == null) return;
            PlayerData local = null;
            foreach (PlayerData p in gameManager.activePlayers)
                if (!p.isAI) { local = p; break; }
            if (local == null) return;

            float barH = 32f;
            float barW = Screen.width * 0.65f;
            float barX = (Screen.width - barW) * 0.5f;

            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(barX - 8, 0, barW + 16, barH + 4), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float cx = barX + 10f;

            GUI.color = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(cx, 6f, 140f, 22f), $"{L("hud_gold")} : {local.money}", titleStyle);
            cx += 150f;

            GUI.color = new Color(0.5f, 0.9f, 0.3f);
            GUI.Label(new Rect(cx, 6f, 140f, 22f), $"{L("hud_wood")} : {local.wood}", titleStyle);
            cx += 150f;

            GUI.color = local.playerColor;
            GUI.Label(new Rect(cx, 6f, 130f, 22f), $"{L("hud_camps")} : {local.ownedCamps.Count}", titleStyle);
            cx += 140f;

            Color popColor = cachedPop >= cachedPopCap ? new Color(1f, 0.35f, 0.35f) : new Color(0.7f, 1f, 0.7f);
            GUI.color = popColor;
            GUI.Label(new Rect(cx, 6f, 130f, 22f), $"Pop : {cachedPop}/{cachedPopCap}", titleStyle);

            GUI.color = Color.white;

            if (LocalizationManager.Instance != null)
            {
                string cur = LocalizationManager.Instance.CurrentLanguage;
                string next = cur == "fr" ? "en" : cur == "en" ? "es" : "fr";
                if (GUI.Button(new Rect(Screen.width - 52f, 4f, 44f, 24f), next.ToUpper(), titleStyle))
                    LocalizationManager.Instance.LoadLanguage(next);
            }
        }

        private void DrawLeaderboard()
        {
            if (gameManager.activePlayers == null) return;
            float rowH = 40f;
            float panelHeight = HeaderHeight + gameManager.activePlayers.Length * rowH + Margin;
            float x = Screen.width - PanelWidth - Margin;
            float y = Margin + 40f;

            GUI.Box(new Rect(x - 5, y - 5, PanelWidth + 10, panelHeight), "", boxStyle);
            GUI.Label(new Rect(x, y, PanelWidth, HeaderHeight), L("hud_title"), titleStyle);
            y += HeaderHeight;

            foreach (PlayerData player in gameManager.activePlayers)
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

        private void DrawUnitStats()
        {
            UnitStats u = InputManager.Instance?.SelectedUnitStats;
            if (u == null) return;

            const float w = 260f;
            const float h = 170f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - h - 10f;

            GUI.Box(new Rect(x - 5, y - 5, w + 10, h + 10), "", boxStyle);

            PlayerData owner = GameManager.Instance?.GetPlayerById(u.ownerId);
            Color prev = GUI.color;
            GUI.color = owner != null ? owner.playerColor : Color.white;
            GUI.Label(new Rect(x, y, w, 22f), $"{UnitDefaults.GetName(u.unitType)}  —  {(owner != null ? owner.playerName : L("hud_neutral"))}", titleStyle);
            GUI.color = prev;
            y += 24f;

            float ratio = u.maxHealth > 0 ? (float)u.currentHealth / u.maxHealth : 1f;
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, w, 20f), Texture2D.whiteTexture);
            GUI.color = Color.Lerp(Color.red, Color.green, ratio);
            GUI.DrawTexture(new Rect(x, y, w * ratio, 20f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y + 2f, w, 20f), $"  {u.currentHealth} / {u.maxHealth}", rowStyle);
            y += 24f;

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

            bool inRegion = RegionManager.Instance != null && RegionManager.Instance.IsInOwnedRegion(u.transform.position, u.ownerId);
            if (inRegion)
            {
                GUI.color = new Color(1f, 1f, 0.3f);
                GUI.Label(new Rect(x, y, w, 18f), $"  {L("hud_region_bonus")}", rowStyle);
                GUI.color = Color.white;
                y += 18f;
            }

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
                        GUI.color = new Color(1f, 0.4f, 0.4f, 0.8f);
                        GUI.Label(new Rect(x + w * 0.55f, y, w * 0.45f, 20f), L("disembark_refused"), rowStyle);
                        GUI.color = Color.white;
                    }
                }
                y += 22f;
            }
        }

        private void DrawBuildingInfo()
        {
            Camp campBldg         = InputManager.Instance?.SelectedCampBuilding;
            BuildingHealth hpBldg = InputManager.Instance?.SelectedHealthBuilding;
            if (campBldg == null && hpBldg == null) return;

            string nameKey;
            int cur, max;

            if (campBldg != null)
            {
                nameKey = campBldg.campType == CampType.Port   ? "building_port"
                        : campBldg.campType == CampType.Castle ? "building_castle"
                        :                                        "building_camp";
                cur = campBldg.currentHP;
                max = campBldg.maxHP;
            }
            else
            {
                nameKey = "building_sawmill";
                cur     = hpBldg.currentHP;
                max     = hpBldg.maxHP;
            }

            const float w = 200f;
            const float h = 60f;
            float x = (Screen.width  - w) * 0.5f;
            float y = Screen.height - h - 10f;

            GUI.Box(new Rect(x - 5, y - 5, w + 10, h + 10), "", boxStyle);

            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, w, 22f), L(nameKey), titleStyle);
            y += 24f;

            float ratio = max > 0 ? (float)cur / max : 1f;
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, w, 20f), Texture2D.whiteTexture);
            GUI.color = Color.Lerp(Color.red, Color.green, ratio);
            GUI.DrawTexture(new Rect(x, y, w * ratio, 20f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y + 2f, w, 20f), $"  {cur} / {max}", rowStyle);

            GUI.color = Color.white;
        }

        private void DrawSunkNotification()
        {
            const float w = 320f;
            float lineH = 18f;
            float h = 28f + sunkPassengers.Count * lineH + 8f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.3f;

            float alpha = Mathf.Clamp01(sunkNotifTimer / 1.5f);
            GUI.color = new Color(0f, 0f, 0f, 0.75f * alpha);
            GUI.DrawTexture(new Rect(x - 8, y - 8, w + 16, h + 16), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.3f, 0.3f, alpha);
            GUI.Label(new Rect(x, y, w, 26f), $"⚓ {L("transport_sunk")}", sunkStyle ?? titleStyle);
            y += 28f;

            GUI.color = new Color(1f, 0.85f, 0.85f, alpha);
            GUI.Label(new Rect(x, y, w, lineH), $"  {L("transport_lost_units")} :", rowStyle);
            y += lineH;

            foreach (string name in sunkPassengers)
            {
                GUI.Label(new Rect(x + 10f, y, w, lineH), $"• {name}", rowStyle);
                y += lineH;
            }
            GUI.color = Color.white;
        }

        private void DrawEndScreen()
        {
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

        private static string L(string key) => LocalizationManager.Get(key);

        private static Vector3 FindNearLand(Vector3 from, float range)
        {
            Collider[] hits = Physics.OverlapSphere(from, range);
            float bestDist = float.MaxValue;
            Vector3 best = Vector3.zero;
            foreach (Collider c in hits)
            {
                HexTile tile = c.GetComponentInParent<HexTile>();
                if (tile == null || tile.terrain != HexTerrain.Walkable) continue;
                float d = Vector3.Distance(from, tile.transform.position);
                if (d < bestDist) { bestDist = d; best = tile.transform.position; }
            }
            return best;
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

            sunkStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 15,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1f, 0.3f, 0.3f) }
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
