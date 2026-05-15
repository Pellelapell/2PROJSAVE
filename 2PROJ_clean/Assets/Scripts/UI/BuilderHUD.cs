using UnityEngine;

namespace SupKonQuest
{
    public class BuilderHUD : MonoBehaviour
    {
        public static BuilderHUD Instance { get; private set; }

        private UnitStats trackedUnit;
        private BuildingType? pendingType;

        public bool HasPendingBuild => pendingType.HasValue;

        private GUIStyle panelStyle, titleStyle, btnStyle, disabledStyle, costStyle, hintStyle;
        private bool stylesReady;

        private static readonly BuildingType[] Types = { BuildingType.Camp, BuildingType.Sawmill, BuildingType.Port, BuildingType.Castle };
        private static readonly string[] Names = { "Camp", "Scierie", "Port", "Château" };
        private static readonly string[] Descs = { "Unités terrestres", "Génère du Bois", "Unités navales (eau)", "Unités spéciales" };

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            // Annuler le placement via ESC ou clic droit
            if (pendingType.HasValue && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
                pendingType = null;

            if (trackedUnit != null && trackedUnit.gameObject == null)
                Hide();
        }

        // ── API ──────────────────────────────────────────────────────

        public void ShowForUnit(UnitStats stats)
        {
            trackedUnit = stats;
            pendingType = null;
        }

        public void Hide()
        {
            trackedUnit  = null;
            pendingType  = null;
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
                // Déplacer le fantassin vers le chantier
                UnitMovement mov = trackedUnit.GetComponent<UnitMovement>();
                mov?.MoveTo(tile.transform.position);
                pendingType = null;
            }
            // Si échec (hors territoire, pas d'eau…) on garde pendingType pour que le joueur re-clique
            return built;
        }

        // ── Rendu ────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (trackedUnit == null || BuildingManager.Instance == null) return;
            InitStyles();

            PlayerData owner = GameManager.Instance?.GetPlayerById(trackedUnit.ownerId);
            if (owner == null) return;

            if (pendingType.HasValue) { DrawPendingHint(); return; }

            DrawBuildPanel(owner);
        }

        private void DrawBuildPanel(PlayerData owner)
        {
            const float w     = 258f;
            const float lineH = 52f;
            float h = 28f + Types.Length * lineH + 8f;
            float x = 10f;
            float y = Screen.height - h - 10f;

            GUI.Box(new Rect(x - 6, y - 6, w + 12, h + 12), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 4, y, w, 24f), "Fantassins — Construire", titleStyle);
            y += 28f;

            for (int i = 0; i < Types.Length; i++)
            {
                BuildingType type = Types[i];
                var (gold, wood) = BuildingManager.Instance.GetCost(type);
                bool canAfford   = owner.CanAfford(gold, wood);

                // Fond de ligne
                GUI.Box(new Rect(x, y, w, lineH - 4f), GUIContent.none, panelStyle);

                // Nom + description
                GUI.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(new Rect(x + 6, y + 2f,  w - 70f, 18f), Names[i], titleStyle);
                GUI.Label(new Rect(x + 6, y + 19f, w - 70f, 14f), Descs[i], costStyle);

                // Coût
                GUI.color = new Color(1f, 0.85f, 0.2f);
                GUI.Label(new Rect(x + 6, y + 33f, w - 70f, 14f), $"{gold}g  {wood}b", costStyle);
                GUI.color = Color.white;

                // Bouton
                if (canAfford)
                {
                    if (GUI.Button(new Rect(x + w - 62f, y + 10f, 58f, 26f), "Poser", btnStyle))
                        pendingType = type;
                }
                else
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.3f);
                    GUI.Box(new Rect(x + w - 62f, y + 10f, 58f, 26f), "Manque", disabledStyle);
                    GUI.color = Color.white;
                }

                y += lineH;
            }
        }

        private void DrawPendingHint()
        {
            int idx = System.Array.IndexOf(Types, pendingType.Value);
            string name = idx >= 0 ? Names[idx] : pendingType.Value.ToString();

            const float w = 340f;
            const float h = 58f;
            float x = (Screen.width  - w) * 0.5f;
            float y = Screen.height - h - 10f;

            GUI.Box(new Rect(x - 6, y - 6, w + 12, h + 12), GUIContent.none, panelStyle);

            GUI.color = new Color(0.4f, 0.95f, 1f);
            GUI.Label(new Rect(x + 6, y + 4f, w, 22f), $"Cliquez sur une case → {name}", hintStyle);
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(x + 6, y + 24f, w, 16f), "Clic droit ou Échap pour annuler", costStyle);
            GUI.color = Color.white;

            if (GUI.Button(new Rect(x + (w - 80f) * 0.5f, y + 32f, 80f, 20f), "Annuler", btnStyle))
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
