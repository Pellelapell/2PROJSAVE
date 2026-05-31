using UnityEngine;

namespace SupKonQuest
{
    public class CampUIManager : MonoBehaviour
    {
        [Header("Legacy Canvas (peut être supprimé de la scène)")]
        [SerializeField] private GameObject panel;

        private Camp           selectedCamp;
        private CampProduction selectedProduction;
        private bool           isVisible;
        private bool           _isPickingSpawnPoint;

        public bool IsPickingSpawnPoint => _isPickingSpawnPoint;

        private GUIStyle panelStyle, titleStyle, btnStyle, disabledStyle, costStyle, hintStyle;
        private bool     stylesReady;

        private static readonly UnitType[] NormalUnits  = { UnitType.Infantry, UnitType.Range, UnitType.Support };
        private static readonly UnitType[] SpecialUnits = { UnitType.AntiArmor, UnitType.Mortar, UnitType.Support };
        private static readonly UnitType[] PortUnits    = { UnitType.Transport, UnitType.Frigate, UnitType.Destroyer };
        private static readonly UnitType[] CastleUnits  = { UnitType.AntiArmor, UnitType.Mortar, UnitType.Heal, UnitType.Heavy };

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            foreach (UnityEngine.UI.Graphic g in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                g.enabled = false;
            foreach (Canvas c in GetComponentsInChildren<Canvas>(true))
                c.enabled = false;
        }

        private void Update()
        {
            if (_isPickingSpawnPoint && Input.GetKeyDown(KeyCode.Escape))
                CancelPickingSpawnPoint();
        }

        public void SelectCamp(Camp camp)
        {
            _isPickingSpawnPoint = false;
            selectedCamp        = camp;
            selectedProduction  = camp != null ? camp.GetComponent<CampProduction>() : null;
            isVisible           = selectedCamp != null && selectedProduction != null;
        }

        public void HideUI()
        {
            _isPickingSpawnPoint = false;
            isVisible           = false;
            selectedCamp        = null;
            selectedProduction  = null;
        }

        public void StartPickingSpawnPoint()
        {
            if (selectedCamp == null) return;
            _isPickingSpawnPoint = true;
        }

        public void CancelPickingSpawnPoint() => _isPickingSpawnPoint = false;

        public void ConfirmSpawnPoint(Vector3 worldPos)
        {
            if (selectedCamp != null) selectedCamp.SetSpawnPosition(worldPos);
            _isPickingSpawnPoint = false;
        }

        private void OnGUI()
        {
            if (_isPickingSpawnPoint) { DrawPickSpawnHint(); return; }
            if (!isVisible || selectedCamp == null || selectedProduction == null) return;
            InitStyles();
            DrawProductionPanel();
        }

        private void DrawProductionPanel()
        {
            UnitType[] units = GetUnitTypes();

            const float lineH  = 70f;
            const float w      = 270f;
            const float infoH  = 50f;
            float h = 28f + units.Length * lineH + 8f + infoH;
            float x = 10f;
            float y = Screen.height - h - 10f;

            string campName = L(selectedCamp.campType == CampType.Port   ? "building_port"
                              : selectedCamp.campType == CampType.Castle ? "building_castle"
                              :                                            "building_camp");

            GUI.Box(new Rect(x - 6, y - 6, w + 12, h + 12), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 4, y, w, 24f), campName, titleStyle);
            y += 28f;

            PlayerData owner = selectedCamp.owner;

            for (int i = 0; i < units.Length; i++)
            {
                UnitType type      = units[i];
                int      price     = UnitDefaults.GetPrice(type);
                float    buildTime = UnitDefaults.GetBuildTime(type);
                string   name      = UnitDefaults.GetName(type);
                bool     canAfford = owner != null && owner.money >= price;

                GUI.Box(new Rect(x, y, w, lineH - 4f), GUIContent.none, panelStyle);

                GUI.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(new Rect(x + 6, y + 5f,  w - 72f, 20f), name, titleStyle);

                GUI.color = new Color(0.65f, 0.65f, 0.65f);
                GUI.Label(new Rect(x + 6, y + 26f, w - 72f, 16f), $"{buildTime:F0}s", costStyle);

                GUI.color = new Color(1f, 0.85f, 0.2f);
                GUI.Label(new Rect(x + 6, y + 44f, w - 72f, 16f), $"{price}g", costStyle);
                GUI.color = Color.white;

                if (canAfford)
                {
                    if (GUI.Button(new Rect(x + w - 66f, y + 19f, 60f, 28f), L("builder_place"), btnStyle))
                        selectedProduction.Produce(type);
                }
                else
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.3f);
                    GUI.Box(new Rect(x + w - 66f, y + 19f, 60f, 28f), L("builder_lack"), disabledStyle);
                    GUI.color = Color.white;
                }

                y += lineH;
            }

            GUI.color = new Color(0.35f, 0.35f, 0.35f, 0.8f);
            GUI.DrawTexture(new Rect(x, y + 2f, w, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            y += 8f;

            UnitType? current = selectedProduction.CurrentType();
            GUI.Label(new Rect(x + 4, y, w - 8f, 16f),
                current.HasValue ? $"{L("hud_producing")} : {UnitDefaults.GetName(current.Value)}"
                                 : L("hud_idle"),
                costStyle);
            y += 18f;

            float progress = selectedProduction.GetProgress01();
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, w, 12f), Texture2D.whiteTexture);
            if (progress > 0f)
            {
                GUI.color = Color.Lerp(new Color(0.15f, 0.55f, 0.2f), new Color(0.1f, 0.9f, 0.3f), progress);
                GUI.DrawTexture(new Rect(x, y, w * progress, 12f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            y += 16f;

            int queue = selectedProduction.GetQueueCount();
            GUI.color = new Color(0.65f, 0.65f, 0.65f);
            GUI.Label(new Rect(x + 4, y, w, 16f), $"{L("hud_queue")} : {queue}", costStyle);
            GUI.color = Color.white;
        }

        private void DrawPickSpawnHint()
        {
            InitStyles();
            const float w = 360f, h = 44f;
            float x = (Screen.width  - w) * 0.5f;
            float y = Screen.height - h - 10f;

            GUI.Box(new Rect(x - 6, y - 6, w + 12, h + 12), GUIContent.none, panelStyle);
            GUI.color = new Color(0.4f, 0.95f, 1f);
            string hint = L("spawn_pick_hint");
            if (string.IsNullOrEmpty(hint) || hint == "spawn_pick_hint")
                hint = "Clic sur une tuile pour définir l'apparition";
            GUI.Label(new Rect(x + 6, y + 4f, w - 80f, 22f), hint, hintStyle);
            GUI.color = Color.white;
            if (GUI.Button(new Rect(x + w - 74f, y + 8f, 70f, 24f), L("builder_cancel"), hintStyle))
                CancelPickingSpawnPoint();
        }

        private UnitType[] GetUnitTypes()
        {
            if (selectedCamp == null) return NormalUnits;
            switch (selectedCamp.campType)
            {
                case CampType.Port:    return PortUnits;
                case CampType.Castle:  return CastleUnits;
                default:               return NormalUnits;
            }
        }

        private void InitStyles()
        {
            if (stylesReady) return;
            stylesReady   = true;
            panelStyle    = new GUIStyle(GUI.skin.box)    { normal = { background = MakeTex(new Color(0.05f, 0.05f, 0.12f, 0.95f)) } };
            titleStyle    = new GUIStyle(GUI.skin.label)  { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            btnStyle      = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold };
            disabledStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, normal   = { textColor = new Color(0.5f, 0.5f, 0.5f) } };
            costStyle     = new GUIStyle(GUI.skin.label)  { fontSize = 11, normal   = { textColor = new Color(0.75f, 0.75f, 0.75f) } };
            hintStyle     = new GUIStyle(GUI.skin.label)  { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        }

        private static string L(string key) => LocalizationManager.Get(key);

        private static Texture2D MakeTex(Color col)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }
    }
}
