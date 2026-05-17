using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SupKonQuest
{
    public class SpellUI : MonoBehaviour
    {
        [Header("References")]
        public GameObject spellPanel;
        public Button spellButton;
        public Image cooldownOverlay;
        public TextMeshProUGUI spellLabel;
        public TextMeshProUGUI cooldownText;

        private UnitSpell trackedSpell;

        private void Update()
        {
            if (trackedSpell == null)
            {
                HidePanel();
                return;
            }

            if (trackedSpell.IsActive)
            {
                cooldownOverlay.fillAmount = 1f - trackedSpell.DurationProgress;
                cooldownText.text = "Actif";
                spellButton.interactable = false;
            }
            else if (trackedSpell.IsOnCooldown)
            {
                cooldownOverlay.fillAmount = 1f - trackedSpell.CooldownProgress;
                cooldownText.text = "Cooldown";
                spellButton.interactable = false;
            }
            else
            {
                cooldownOverlay.fillAmount = 0f;
                cooldownText.text = "Pr�t";
                spellButton.interactable = true;
            }
        }

        public void ShowForUnit(UnitSpell spell)
        {
            UnitStats stats = spell != null ? spell.GetComponentInParent<UnitStats>() : null;
            if (stats == null || !stats.hasActivable)
            {
                HidePanel();
                return;
            }

            trackedSpell = spell;
            spellPanel.SetActive(true);

            spellLabel.text = stats.unitType == UnitType.Heal
                ? LocalizationManager.Get("unit_heal")
                : LocalizationManager.Get("unit_support");

            spellButton.onClick.RemoveAllListeners();
            spellButton.onClick.AddListener(() => trackedSpell.TryActivate());
        }

        public void HidePanel()
        {
            trackedSpell = null;
            if (spellPanel != null)
                spellPanel.SetActive(false);
        }
    }
}