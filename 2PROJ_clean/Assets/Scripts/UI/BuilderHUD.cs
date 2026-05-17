using UnityEngine;

namespace SupKonQuest
{
    public class BuilderHUD : MonoBehaviour
    {
        public static BuilderHUD Instance { get; private set; }

        private UnitStats trackedUnit;
        private BuildingType? pendingType;
        private bool justActivated;

        // Séparé de trackedUnit : reste actif même après déselection
        private UnitStats builderUnit;
        private HexTile   activeSiteTile;
        private bool      builderLocked;

        public bool HasPendingBuild => pendingType.HasValue && !justActivated;

        public bool IsMouseOverPanel
        {
            get
            {
                if (trackedUnit == null) return false;
                float mx  = Input.mousePosition.x;
                float mgy = Screen.height - Input.mousePosition.y;

                float bh = 28f + Types.Length * 52f + 8f + 12f;
                float by = Screen.height - bh - 10f;
                if (new Rect(4f, by, 270f, bh).Contains(new Vector2(mx, mgy))) return true;

                float pw = 352f, ph = 70f;
                float px = (Screen.width - pw) * 0.5f;
                float py = Screen.height - ph - 10f;
                return new Rect(px, py, pw, ph).Contains(new Vector2(mx, mgy));
            }
        }

        private GUIStyle panelStyle, titleStyle, btnStyle, disabledStyle, costStyle, hintStyle;
        private bool stylesReady;

        private static readonly BuildingType[] Types = { BuildingType.Camp, BuildingType.Sawmill, BuildingType.Port, BuildingType.Castle };

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

            UpdateBuilderState();
        }

        private void UpdateBuilderState()
        {
            if (activeSiteTile == null || builderUnit == null) return;

            // Builder mort → annuler chantier
            if (builderUnit.gameObject == null) { CancelAndRelease(); return; }

            // Construction terminée → libérer sans annuler
            if (BuildingManager.Instance == null || BuildingManager.Instance.GetProgress01(activeSiteTile) < 0f)
                ReleaseBuilder();
        }

        private void ReleaseBuilder()
        {
            if (builderUnit != null)
            {
                UnitMovement mov = builderUnit.GetComponent<UnitMovement>();
                if (mov != null) mov.IsLocked = false;
            }
            builderUnit    = null;
            activeSiteTile = null;
            builderLocked  = false;
        }

        private void CancelAndRelease()
        {
            if (activeSiteTile != null)
                BuildingManager.Instance?.CancelBuild(activeSiteTile);
            ReleaseBuilder();
        }

        private void LateUpdate()
        {
            justActivated = false;
        }

        // ── API ──────────────────────────────────────────────────────

        public void ShowForUnit(UnitStats stats)
        {
            trackedUnit = stats;
            pendingType = null;
        }

        public void Hide()
        {
            trackedUnit = null;
            pendingType = null;
            // builderUnit et activeSiteTile restent actifs jusqu'à la fin du chantier
        }

        public void CancelPending() => pendingType = null;

        public bool TryPlaceOnTile(HexTile tile)
        {
            if (!pendingType.HasValue || trackedUnit == null || BuildingManager.Instance == null) return false;

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
                builderUnit    = trackedUnit;
                activeSiteTile = tile;
                builderLocked  = true;
                pendingType    = null;
            }
            return built;
        }

        // ── Rendu ────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawConstructionProgress();

            if (trackedUnit == null || BuildingManager.Instance == null) return;
            InitStyles();

            PlayerData owner = GameManager.Instance?.GetPlayerById(trackedUnit.ownerId);
            if (owner == null) return;

            if (pendingType.HasValue) { DrawPendingHint(); return; }

            DrawBuildPanel(owner);
        }

        // Barre de progression de construction au-dessus de l'infanterie
        // Reste visible même après déselection tant que le chantier est actif
        private void DrawConstructionProgress()
        {
            if (activeSiteTile == null || builderUnit == null || BuildingManager.Instance == null) return;
            if (builderUnit.gameObject == null) return;

            float progress = BuildingManager.Instance.GetProgress01(activeSiteTile);
            if (progress < 0f) return; // cleanup géré dans Update

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 sp = cam.WorldToScreenPoint(builderUnit.transform.position + Vector3.up * 2.5f);
            if (sp.z < 0f) return;

            const float bw = 80f, bh = 10f;
            float bx = sp.x - bw * 0.5f;
            float by = Screen.height - sp.y - bh - 4f;

            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            GUI.DrawTexture(new Rect(bx, by, bw, bh), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.55f, 0f);
            GUI.DrawTexture(new Rect(bx, by, bw * progress, bh), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawBuildPanel(PlayerData owner)
        {
            const float w     = 258f;
            const float lineH = 52f;
            float h = 28f + Types.Length * lineH + 8f;
            float x = 10f;
            float y = Screen.height - h - 10f;

            GUI.Box(new Rect(x - 6, y - 6, w + 12, h + 12), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 4, y, w, 24f), L("builder_title"), titleStyle);
            y += 28f;

            for (int i = 0; i < Types.Length; i++)
            {
                BuildingType type = Types[i];
                var (gold, wood) = BuildingManager.Instance.GetCost(type);
                bool canAfford   = owner.CanAfford(gold, wood);

                GUI.Box(new Rect(x, y, w, lineH - 4f), GUIContent.none, panelStyle);

                GUI.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(new Rect(x + 6, y + 2f,  w - 70f, 18f), GetBuildingName(i), titleStyle);
                GUI.Label(new Rect(x + 6, y + 19f, w - 70f, 14f), GetBuildingDesc(i), costStyle);

                GUI.color = new Color(1f, 0.85f, 0.2f);
                GUI.Label(new Rect(x + 6, y + 33f, w - 70f, 14f), $"{gold}g  {wood}b", costStyle);
                GUI.color = Color.white;

                if (canAfford)
                {
                    if (GUI.Button(new Rect(x + w - 62f, y + 10f, 58f, 26f), L("builder_place"), btnStyle))
                    {
                        pendingType   = type;
                        justActivated = true;
                    }
                }
                else
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.3f);
                    GUI.Box(new Rect(x + w - 62f, y + 10f, 58f, 26f), L("builder_lack"), disabledStyle);
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
            float x = (Screen.width  - w) * 0.5f;
            float y = Screen.height - h - 10f;

            GUI.Box(new Rect(x - 6, y - 6, w + 12, h + 12), GUIContent.none, panelStyle);

            GUI.color = new Color(0.4f, 0.95f, 1f);
            GUI.Label(new Rect(x + 6, y + 4f, w, 22f), $"{L("builder_hint_place")} {name}", hintStyle);
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(x + 6, y + 24f, w, 16f), L("builder_hint_cancel"), costStyle);
            GUI.color = Color.white;

            if (GUI.Button(new Rect(x + (w - 80f) * 0.5f, y + 32f, 80f, 20f), L("builder_cancel"), btnStyle))
                pendingType = null;
        }

        // ── Styles ───────────────────────────────────────────────────

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
