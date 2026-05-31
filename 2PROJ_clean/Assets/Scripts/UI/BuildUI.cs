using UnityEngine;

namespace SupKonQuest
{
    public class BuildUI : MonoBehaviour
    {
        public static BuildUI Instance { get; private set; }

        private HexTile selectedTile;
        private PlayerData localPlayer;

        private GUIStyle panelStyle, titleStyle, btnStyle, disabledStyle, costStyle;
        private bool stylesReady;

        private static readonly BuildingType[] Types = { BuildingType.Camp, BuildingType.Sawmill, BuildingType.Port, BuildingType.Castle };

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            localPlayer = FindLocalPlayer();
        }

        public void SelectTile(HexTile tile) => selectedTile = tile;
        public void Deselect()               => selectedTile = null;
        public HexTile SelectedTile          => selectedTile;

        private void OnGUI()
        {
            if (selectedTile == null || BuildingManager.Instance == null) return;
            if (localPlayer == null) { localPlayer = FindLocalPlayer(); return; }
            InitStyles();

            Matrix4x4 oldMatrix = GUI.matrix;
            float s = HUDManager.HudScale;
            GUIUtility.ScaleAroundPivot(new Vector2(s, s), Vector2.zero);
            float sw = Screen.width  / s;
            float sh = Screen.height / s;

            float w = 280f;
            float lineH = 72f;
            float h = 30f + Types.Length * lineH + 10f;
            float x = (sw - w) * 0.5f;
            float y = sh - h - 120f;

            GUI.Box(new Rect(x - 6, y - 6, w + 12, h + 12), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x, y, w, 24f), "  Construire", titleStyle);
            y += 28f;

            foreach (BuildingType type in Types)
            {
                var (gold, wood) = BuildingManager.Instance.GetCost(type);
                bool canBuild = BuildingManager.Instance.CanBuild(selectedTile, type, localPlayer);
                GUIStyle style = canBuild ? btnStyle : disabledStyle;
                string typeKey = type.ToString().ToLower();

                GUI.Box(new Rect(x, y, w, lineH - 4f), GUIContent.none, panelStyle);
                GUI.Label(new Rect(x + 6, y + 2,  w - 12, 20f), LocalizationManager.Get("building_" + typeKey), titleStyle);
                GUI.Label(new Rect(x + 6, y + 20f, w - 12, 16f), LocalizationManager.Get("builder_desc_" + typeKey), costStyle);

                GUI.color = new Color(1f, 0.85f, 0.2f);
                GUI.Label(new Rect(x + 6, y + 46f, 130f, 18f), $"Or: {gold}  Bois: {wood}", costStyle);
                GUI.color = Color.white;

                if (canBuild && GUI.Button(new Rect(x + w - 80f, y + 38f, 74f, 26f), "Construire", style))
                {
                    BuildingManager.Instance.TryBuild(selectedTile, type, localPlayer);
                    selectedTile = null;
                }
                else if (!canBuild)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.4f);
                    GUI.Box(new Rect(x + w - 80f, y + 38f, 74f, 26f), "Manque", disabledStyle);
                    GUI.color = Color.white;
                }

                y += lineH;
            }

            if (GUI.Button(new Rect(x + w - 28f, y - Types.Length * lineH - 32f, 24f, 20f), "✕", costStyle))
                selectedTile = null;

            GUI.matrix = oldMatrix;
        }

        private PlayerData FindLocalPlayer()
        {
            if (GameManager.Instance == null) return null;
            foreach (PlayerData p in GameManager.Instance.activePlayers)
                if (!p.isAI) return p;
            return null;
        }

        private void InitStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(new Color(0.05f, 0.05f, 0.12f, 0.95f)) }
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13, fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11, fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };
            disabledStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
            costStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
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
