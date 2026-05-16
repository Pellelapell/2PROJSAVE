using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SupKonQuest
{
    public class CampUIManager : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panel;
        public TMP_Text titleText;

        [Header("Production Info")]
        public TMP_Text currentUnitText;
        public TMP_Text queueText;
        public Image progressBarFill;

        [Header("Database")]
        public UnitDatabase unitDatabase;

        [Header("Buttons – Camp")]
        public Button infantryButton;
        public Button supportButton;
        public Button healButton;
        public Button rangeButton;
        public Button heavyButton;
        public Button antiArmorButton;
        public Button mortarButton;

        [Header("Buttons – Port")]
        public Button transportButton;
        public Button frigateButton;
        public Button destroyerButton;

        [Header("Spawn Point")]
        public Button spawnPointButton;

        private Camp selectedCamp;
        private CampProduction selectedProduction;
        private bool isPickingSpawnPoint;

        public bool IsPickingSpawnPoint => isPickingSpawnPoint;

        private GUIStyle hintStyle;
        private GUIStyle panelStyle;

        private void Start()
        {
            HideUI();

            BindButton(infantryButton,  UnitType.Infantry);
            BindButton(supportButton,   UnitType.Support);
            BindButton(healButton,      UnitType.Heal);
            BindButton(rangeButton,     UnitType.Range);
            BindButton(heavyButton,     UnitType.Heavy);
            BindButton(antiArmorButton, UnitType.AntiArmor);
            BindButton(mortarButton,    UnitType.Mortar);
            BindButton(transportButton, UnitType.Transport);
            BindButton(frigateButton,   UnitType.Frigate);
            BindButton(destroyerButton, UnitType.Destroyer);

            if (spawnPointButton != null)
                spawnPointButton.onClick.AddListener(StartPickingSpawnPoint);
        }

        private void Update()
        {
            RefreshProductionInfo();

            if (isPickingSpawnPoint && Input.GetKeyDown(KeyCode.Escape))
                CancelPickingSpawnPoint();
        }

        public void SelectCamp(Camp camp)
        {
            isPickingSpawnPoint = false;
            selectedCamp = camp;
            selectedProduction = camp != null ? camp.GetComponent<CampProduction>() : null;

            if (selectedCamp == null || selectedProduction == null)
            {
                HideUI();
                return;
            }

            panel.SetActive(true);
            titleText.text = selectedCamp.campType.ToString();

            RefreshButtons();
            RefreshProductionInfo();
        }

        public void HideUI()
        {
            isPickingSpawnPoint = false;
            selectedCamp = null;
            selectedProduction = null;
            if (panel != null) panel.SetActive(false);
        }

        // ── Spawn point picking ───────────────────────────────────────

        public void StartPickingSpawnPoint()
        {
            if (selectedCamp == null) return;
            isPickingSpawnPoint = true;
        }

        public void CancelPickingSpawnPoint()
        {
            isPickingSpawnPoint = false;
        }

        public void ConfirmSpawnPoint(Vector3 worldPos)
        {
            if (selectedCamp != null)
                selectedCamp.SetSpawnPosition(worldPos);
            isPickingSpawnPoint = false;
        }

        private void OnGUI()
        {
            if (!isPickingSpawnPoint) return;

            InitGuiStyles();

            const float w = 360f, h = 44f;
            float x = (Screen.width  - w) * 0.5f;
            float y = Screen.height - h - 10f;

            GUI.Box(new Rect(x - 6, y - 6, w + 12, h + 12), GUIContent.none, panelStyle);
            GUI.color = new Color(0.4f, 0.95f, 1f);
            GUI.Label(new Rect(x + 6, y + 4f, w - 80f, 22f), LocalizationManager.Get("spawn_pick_hint"), hintStyle);
            GUI.color = Color.white;
            if (GUI.Button(new Rect(x + w - 74f, y + 8f, 70f, 24f), LocalizationManager.Get("builder_cancel"), hintStyle))
                CancelPickingSpawnPoint();
        }

        private void InitGuiStyles()
        {
            if (hintStyle != null) return;
            hintStyle  = new GUIStyle(GUI.skin.label)  { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            panelStyle = new GUIStyle(GUI.skin.box)    { normal   = { background = MakeTex(new Color(0.05f, 0.05f, 0.12f, 0.95f)) } };
        }

        private static Texture2D MakeTex(Color col)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }

        private void RefreshButtons()
        {
            SetButtonActive(infantryButton,  false);
            SetButtonActive(supportButton,   false);
            SetButtonActive(healButton,      false);
            SetButtonActive(rangeButton,     false);
            SetButtonActive(heavyButton,     false);
            SetButtonActive(antiArmorButton, false);
            SetButtonActive(mortarButton,    false);
            SetButtonActive(transportButton, false);
            SetButtonActive(frigateButton,   false);
            SetButtonActive(destroyerButton, false);
            if (selectedCamp == null) return;

            switch (selectedCamp.campType)
            {
                case CampType.Normal:
                    SetButtonActive(infantryButton, true);
                    SetButtonActive(rangeButton,    true);
                    SetButtonActive(supportButton,  true);
                    break;

                case CampType.NeutralSpecial:
                    SetButtonActive(antiArmorButton, true);
                    SetButtonActive(mortarButton,    true);
                    SetButtonActive(supportButton,   true);
                    break;

                case CampType.Port:
                    SetButtonActive(transportButton, true);
                    SetButtonActive(frigateButton,   true);
                    SetButtonActive(destroyerButton, true);
                    break;

                case CampType.Castle:
                    SetButtonActive(antiArmorButton, true);
                    SetButtonActive(mortarButton,    true);
                    SetButtonActive(healButton,      true);
                    SetButtonActive(heavyButton,     true);
                    break;
            }
        }

        private void SetButtonActive(Button btn, bool active)
        {
            if (btn == null) return;
            btn.gameObject.SetActive(active);
            if (active) RefreshButtonLabel(btn);
        }

        private void RefreshButtonLabel(Button btn)
        {
            if (btn == null) return;

            UnitType type  = GetUnitTypeFromButton(btn);
            string name    = UnitDefaults.GetName(type);
            int price      = UnitDefaults.GetPrice(type);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = $"{name}\n<size=70%>{price}g</size>";
        }

        private UnitType GetUnitTypeFromButton(Button btn)
        {
            if (btn == infantryButton)  return UnitType.Infantry;
            if (btn == supportButton)   return UnitType.Support;
            if (btn == healButton)      return UnitType.Heal;
            if (btn == rangeButton)     return UnitType.Range;
            if (btn == heavyButton)     return UnitType.Heavy;
            if (btn == antiArmorButton) return UnitType.AntiArmor;
            if (btn == mortarButton)    return UnitType.Mortar;
            if (btn == transportButton) return UnitType.Transport;
            if (btn == frigateButton)   return UnitType.Frigate;
            if (btn == destroyerButton) return UnitType.Destroyer;
            return UnitType.Infantry;
        }

        private void RefreshProductionInfo()
        {
            if (selectedProduction == null)
            {
                if (currentUnitText  != null) currentUnitText.text  = "";
                if (queueText        != null) queueText.text        = "";
                if (progressBarFill  != null) progressBarFill.fillAmount = 0f;
                return;
            }

            UnitType? currentType = selectedProduction.CurrentType();

            if (currentUnitText != null)
                currentUnitText.text = currentType.HasValue
                    ? "Production : " + UnitDefaults.GetName(currentType.Value)
                    : "Inactif";

            if (queueText != null)
                queueText.text = "File : " + selectedProduction.GetQueueCount();

            if (progressBarFill != null)
                progressBarFill.fillAmount = selectedProduction.GetProgress01();
        }

        private void BindButton(Button btn, UnitType type)
        {
            if (btn == null) return;
            btn.onClick.AddListener(() => Produce(type));
        }

        private void Produce(UnitType type)
        {
            if (selectedProduction == null) return;
            selectedProduction.Produce(type);
            RefreshProductionInfo();
        }
    }
}
