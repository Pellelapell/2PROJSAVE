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

        [Header("Buttons")]
        public Button infantryButton;
        public Button supportButton;
        public Button healButton;
        public Button rangeButton;
        public Button heavyButton;
        public Button antiArmorButton;
        public Button mortarButton;
        public Button transportButton;
        public Button frigateButton;
        public Button destroyerButton;

        private Camp selectedCamp;
        private CampProduction selectedProduction;

        private void Start()
        {
            HideUI();

            BindButton(infantryButton, UnitType.Infantry);
            BindButton(supportButton, UnitType.Support);
            BindButton(healButton, UnitType.Heal);
            BindButton(rangeButton, UnitType.Range);
            BindButton(heavyButton, UnitType.Heavy);
            BindButton(antiArmorButton, UnitType.AntiArmor);
            BindButton(mortarButton, UnitType.Mortar);
            BindButton(transportButton, UnitType.Transport);
            BindButton(frigateButton, UnitType.Frigate);
            BindButton(destroyerButton, UnitType.Destroyer);
        }

        private void Update()
        {
            RefreshProductionInfo();
        }

        public void SelectCamp(Camp camp)
        {
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
            selectedCamp = null;
            selectedProduction = null;
            if (panel != null) panel.SetActive(false);
        }

        private void RefreshButtons()
        {
            SetButtonActive(infantryButton, false);
            SetButtonActive(supportButton, false);
            SetButtonActive(healButton, false);
            SetButtonActive(rangeButton, false);
            SetButtonActive(heavyButton, false);
            SetButtonActive(antiArmorButton, false);
            SetButtonActive(mortarButton, false);
            SetButtonActive(transportButton, false);
            SetButtonActive(frigateButton, false);
            SetButtonActive(destroyerButton, false);

            if (selectedCamp == null) return;

            switch (selectedCamp.campType)
            {
                case CampType.Normal:
                    // Unités terrestres de base
                    SetButtonActive(infantryButton,  true);
                    SetButtonActive(rangeButton,     true);
                    SetButtonActive(heavyButton,     true);
                    SetButtonActive(healButton,      true);
                    break;

                case CampType.NeutralSpecial:
                    // Unités spéciales / tactiques
                    SetButtonActive(antiArmorButton, true);
                    SetButtonActive(mortarButton,    true);
                    SetButtonActive(supportButton,   true);
                    break;

                case CampType.Port:
                    // Unités navales uniquement
                    SetButtonActive(transportButton, true);
                    SetButtonActive(frigateButton,   true);
                    SetButtonActive(destroyerButton, true);
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
            if (btn == null || unitDatabase == null) return;

            UnitType type = GetUnitTypeFromButton(btn);
            UnitDefinition def = unitDatabase.Get(type);
            if (def == null) return;

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = $"{def.displayName}\n<size=70%>{def.price}g</size>";
        }

        private UnitType GetUnitTypeFromButton(Button btn)
        {
            if (btn == infantryButton) return UnitType.Infantry;
            if (btn == supportButton) return UnitType.Support;
            if (btn == healButton) return UnitType.Heal;
            if (btn == rangeButton) return UnitType.Range;
            if (btn == heavyButton) return UnitType.Heavy;
            if (btn == antiArmorButton) return UnitType.AntiArmor;
            if (btn == mortarButton) return UnitType.Mortar;
            if (btn == transportButton) return UnitType.Transport;
            if (btn == frigateButton) return UnitType.Frigate;
            if (btn == destroyerButton) return UnitType.Destroyer;
            return UnitType.Infantry;
        }

        private void RefreshProductionInfo()
        {
            if (selectedProduction == null)
            {
                if (currentUnitText != null) currentUnitText.text = "";
                if (queueText != null) queueText.text = "";
                if (progressBarFill != null) progressBarFill.fillAmount = 0f;
                return;
            }

            UnitType? currentType = selectedProduction.CurrentType();

            if (currentUnitText != null)
                currentUnitText.text = currentType.HasValue ? "Production : " + currentType.Value : "Inactif";

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