using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    public class BuilderHUD : MonoBehaviour
    {
        public static BuilderHUD Instance { get; private set; }

        private UnitStats    trackedUnit;
        private BuildingType? pendingType;
        private bool          justActivated;

        private class BuildSite
        {
            public UnitStats builder;
            public HexTile   tile;
            public bool      started;
        }

        private readonly List<BuildSite> activeSites = new List<BuildSite>();

        public bool HasPendingBuild => pendingType.HasValue && !justActivated;

        public bool IsMouseOverPanel
        {
            get
            {
                if (trackedUnit == null) return false;
                float s   = HUDManager.HudScale;
                float sh  = Screen.height / s;
                float sw  = Screen.width  / s;
                float mx  = Input.mousePosition.x / s;
                float mgy = (Screen.height - Input.mousePosition.y) / s;

                float bh = 28f + Types.Length * 72f + 8f + 12f;
                float by = sh - bh - 10f;
                if (new Rect(4f, by, 272f, bh).Contains(new Vector2(mx, mgy))) return true;

                float pw = 352f, ph = 70f;
                float px = (sw - pw) * 0.5f;
                float py = sh - ph - 10f;
                return new Rect(px, py, pw, ph).Contains(new Vector2(mx, mgy));
            }
        }

        private GUIStyle panelStyle, titleStyle, btnStyle, disabledStyle, costStyle, hintStyle;
        private bool     stylesReady;

        private float _sw, _sh, _s;

        private static readonly BuildingType[] Types =
            { BuildingType.Camp, BuildingType.Sawmill, BuildingType.Port, BuildingType.Castle };

        private static string L(string key) => LocalizationManager.Get(key);

        private static string GetBuildingName(int i)
        {
            switch (i)
            {
                case 0: return L("building_camp");
                case 1: return L("building_sawmill");
                case 2: return L("building_port");
                case 3: return L("building_castle");
                default: return Types[i].ToString();
            }
        }

        private static string GetBuildingDesc(int i)
        {
            switch (i)
            {
                case 0: return L("builder_desc_camp");
                case 1: return L("builder_desc_sawmill");
                case 2: return L("builder_desc_port");
                case 3: return L("builder_desc_castle");
                default: return "";
            }
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (pendingType.HasValue && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
                pendingType = null;

            if (trackedUnit != null && trackedUnit.gameObject == null)
                Hide();

            UpdateBuilderStates();
        }

        private void UpdateBuilderStates()
        {
            if (BuildingManager.Instance == null) return;

            for (int i = activeSites.Count - 1; i >= 0; i--)
            {
                BuildSite site = activeSites[i];

                if (site.builder == null || site.builder.gameObject == null)
                {
                    BuildingManager.Instance.CancelBuild(site.tile);
                    activeSites.RemoveAt(i);
                    continue;
                }

                float progress = BuildingManager.Instance.GetProgress01(site.tile);
                if (progress < 0f)
                {
                    ReleaseSite(site);
                    activeSites.RemoveAt(i);
                    continue;
                }

                if (!site.started)
                {
                    UnitMovement mov = site.builder.GetComponent<UnitMovement>();
                    if (mov != null && !mov.IsMoving)
                    {
                        site.started = true;
                        BuildingManager.Instance.BeginConstruction(site.tile);
                    }
                }
            }
        }

        public void NotifyBuildComplete(HexTile tile)
        {
            for (int i = activeSites.Count - 1; i >= 0; i--)
            {
                if (activeSites[i].tile != tile) continue;
                ReleaseSite(activeSites[i]);
                activeSites.RemoveAt(i);
                return;
            }
        }

        private void ReleaseSite(BuildSite site)
        {
            if (site.builder == null) return;
            UnitMovement mov = site.builder.GetComponent<UnitMovement>();
            if (mov != null) mov.IsLocked = false;
        }

        private void LateUpdate() => justActivated = false;

        public void ShowForUnit(UnitStats stats)
        {
            trackedUnit = stats;
            pendingType = null;
        }

        public void Hide()
        {
            trackedUnit = null;
            pendingType = null;
        }

        public void CancelPending() => pendingType = null;

        public bool TryPlaceOnTile(HexTile tile)
        {
            if (!pendingType.HasValue || trackedUnit == null || BuildingManager.Instance == null) return false;

            for (int i = activeSites.Count - 1; i >= 0; i--)
            {
                if (activeSites[i].builder != trackedUnit) continue;
                BuildingManager.Instance.CancelBuild(activeSites[i].tile);
                ReleaseSite(activeSites[i]);
                activeSites.RemoveAt(i);
                break;
            }

            PlayerData owner = GameManager.Instance?.GetPlayerById(trackedUnit.ownerId);
            if (owner == null) { pendingType = null; return false; }

            bool built = BuildingManager.Instance.TryBuild(tile, pendingType.Value, owner);
            if (built)
            {
                UnitMovement mov = trackedUnit.GetComponent<UnitMovement>();
                if (mov != null)
                {
                    mov.MoveToForced(tile.transform.position);
                    mov.IsLocked = true;
                }
                activeSites.Add(new BuildSite { builder = trackedUnit, tile = tile, started = false });
                pendingType = null;
            }
            return built;
        }

        private void OnGUI()
        {
            Matrix4x4 oldMatrix = GUI.matrix;
            _s  = HUDManager.HudScale;
            GUIUtility.ScaleAroundPivot(new Vector2(_s, _s), Vector2.zero);
            _sw = Screen.width  / _s;
            _sh = Screen.height / _s;

            DrawAllProgressBars();

            if (trackedUnit != null && BuildingManager.Instance != null)
            {
                InitStyles();
                PlayerData owner = GameManager.Instance?.GetPlayerById(trackedUnit.ownerId);
                if (owner != null)
                {
                    if (pendingType.HasValue) DrawPendingHint();
                    else                      DrawBuildPanel(owner);
                }
            }

            GUI.matrix = oldMatrix;
        }

        private void DrawAllProgressBars()
        {
            if (BuildingManager.Instance == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            foreach (BuildSite site in activeSites)
            {
                if (site.builder == null) continue;
                float progress = BuildingManager.Instance.GetProgress01(site.tile);
                if (progress < 0f) continue;

                Vector3 sp = cam.WorldToScreenPoint(site.builder.transform.position + Vector3.up * 2.5f);
                if (sp.z < 0f) continue;

                const float bw = 80f, bh = 10f;
                float bx = sp.x / _s - bw * 0.5f;
                float by = (Screen.height - sp.y) / _s - bh - 4f;

                GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
                GUI.DrawTexture(new Rect(bx, by, bw, bh), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.55f, 0f);
                GUI.DrawTexture(new Rect(bx, by, bw * progress, bh), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        private void DrawBuildPanel(PlayerData owner)
        {
            const float w     = 260f;
            const float lineH = 72f;
            float h = 28f + Types.Length * lineH + 8f;
            float x = 10f;
            float y = _sh - h - 10f;

            GUI.Box(new Rect(x - 6, y - 6, w + 12, h + 12), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 4, y, w, 24f), L("builder_title"), titleStyle);
            y += 28f;

            GUIStyle descStyle = new GUIStyle(costStyle) { wordWrap = true };

            for (int i = 0; i < Types.Length; i++)
            {
                BuildingType type = Types[i];
                var (gold, wood) = BuildingManager.Instance.GetCost(type);
                string blockReason = BuildingManager.Instance.GetTypeBlockReason(type, owner);
                bool canBuild = blockReason == null;

                GUI.Box(new Rect(x, y, w, lineH - 4f), GUIContent.none, panelStyle);

                GUI.color = canBuild ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(new Rect(x + 6, y + 4f,  w - 68f, 20f), GetBuildingName(i), titleStyle);
                GUI.Label(new Rect(x + 6, y + 24f, w - 68f, 16f), GetBuildingDesc(i), descStyle);

                GUI.color = new Color(1f, 0.85f, 0.2f);
                GUI.Label(new Rect(x + 6, y + 40f, w - 68f, 16f), $"{gold}g  {wood}b", costStyle);
                GUI.color = Color.white;

                if (canBuild)
                {
                    if (GUI.Button(new Rect(x + w - 62f, y + 20f, 56f, 28f), L("builder_place"), btnStyle))
                    {
                        pendingType   = type;
                        justActivated = true;
                    }
                }
                else
                {
                    GUIStyle reasonStyle = new GUIStyle(costStyle)
                        { normal = { textColor = new Color(1f, 0.4f, 0.4f) }, wordWrap = true };
                    GUI.Label(new Rect(x + 6, y + 54f, w - 12f, 16f), blockReason, reasonStyle);
                    GUI.color = new Color(1f, 1f, 1f, 0.3f);
                    GUI.Box(new Rect(x + w - 62f, y + 20f, 56f, 28f), L("builder_lack"), disabledStyle);
                    GUI.color = Color.white;
                }

                y += lineH;
            }
        }

        private void DrawPendingHint()
        {
            int idx = System.Array.IndexOf(Types, pendingType.Value);
            string name = idx >= 0 ? GetBuildingName(idx) : pendingType.Value.ToString();

            const float w = 340f;
            const float h = 58f;
            float x = (_sw - w) * 0.5f;
            float y = _sh - h - 10f;

            GUI.Box(new Rect(x - 6, y - 6, w + 12, h + 12), GUIContent.none, panelStyle);

            GUI.color = new Color(0.4f, 0.95f, 1f);
            GUI.Label(new Rect(x + 6, y + 4f, w, 22f), $"{L("builder_hint_place")} {name}", hintStyle);
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(x + 6, y + 24f, w, 16f), L("builder_hint_cancel"), costStyle);
            GUI.color = Color.white;

            if (GUI.Button(new Rect(x + (w - 80f) * 0.5f, y + 32f, 80f, 20f), L("builder_cancel"), btnStyle))
                pendingType = null;
        }

        private void InitStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            panelStyle    = new GUIStyle(GUI.skin.box)    { normal = { background = MakeTex(new Color(0.05f, 0.05f, 0.12f, 0.95f)) } };
            titleStyle    = new GUIStyle(GUI.skin.label)  { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            btnStyle      = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold };
            disabledStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, normal   = { textColor = new Color(0.5f, 0.5f, 0.5f) } };
            costStyle     = new GUIStyle(GUI.skin.label)  { fontSize = 11, normal   = { textColor = new Color(0.75f, 0.75f, 0.75f) } };
            hintStyle     = new GUIStyle(GUI.skin.label)  { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        }

        private static Texture2D MakeTex(Color col)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }
    }
}
