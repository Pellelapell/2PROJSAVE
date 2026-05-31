using UnityEngine;

namespace SupKonQuest
{
    public class SpellUI : MonoBehaviour
    {
        // Ancien panel Canvas — désactivé au démarrage, tout est rendu via OnGUI
        [Header("Legacy Canvas (peut être supprimé de la scène)")]
        [SerializeField] private GameObject spellPanel;

        private UnitSpell trackedSpell;
        private bool      isVisible;

        private GUIStyle panelStyle, titleStyle, btnStyle, disabledStyle, costStyle;
        private bool     stylesReady;

        private void Awake()
        {
            // Masquer l'ancienne UI Canvas sans désactiver le GameObject
            // (désactiver le GO tuerait OnGUI)
            foreach (UnityEngine.UI.Graphic g in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                g.enabled = false;
            foreach (Canvas c in GetComponentsInChildren<Canvas>(true))
                c.enabled = false;
        }

        public void ShowForUnit(UnitSpell spell)
        {
            UnitStats stats = spell != null ? spell.GetComponentInParent<UnitStats>() : null;
            if (stats == null || !stats.hasActivable) { HidePanel(); return; }
            trackedSpell = spell;
            isVisible    = true;
        }

        public void HidePanel()
        {
            trackedSpell = null;
            isVisible    = false;
        }

        private void OnGUI()
        {
            if (!isVisible || trackedSpell == null) return;
            UnitStats stats = trackedSpell.GetComponentInParent<UnitStats>();
            if (stats == null) { HidePanel(); return; }
            InitStyles();
            DrawSpellPanel(stats);
        }

        private void DrawSpellPanel(UnitStats stats)
        {
            const float w = 260f;
            const float h = 106f;
            float x = 10f;
            float y = Screen.height - h - 10f;

            string spellName = stats.unitType == UnitType.Heal
                ? LocalizationManager.Get("unit_heal")
                : LocalizationManager.Get("unit_support");

            GUI.Box(new Rect(x - 6, y - 6, w + 12, h + 12), GUIContent.none, panelStyle);

            // Titre
            GUI.Label(new Rect(x + 4, y, w, 24f), spellName, titleStyle);
            y += 28f;

            // Calcul de l'état
            float  fill;
            string statusText;
            Color  barColor;

            if (trackedSpell.IsActive)
            {
                fill       = 1f - trackedSpell.DurationProgress;
                statusText = "Actif";
                barColor   = new Color(0.15f, 0.85f, 0.3f);
            }
            else if (trackedSpell.IsOnCooldown)
            {
                fill       = 1f - trackedSpell.CooldownProgress;
                statusText = "Cooldown";
                barColor   = new Color(0.45f, 0.45f, 0.45f);
            }
            else
            {
                fill       = 1f;
                statusText = "Prêt";
                barColor   = new Color(0.2f, 0.55f, 1f);
            }

            // Barre de statut
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, w, 16f), Texture2D.whiteTexture);
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(x, y, w * fill, 16f), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUIStyle statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };
            GUI.Label(new Rect(x, y, w, 16f), statusText, statusStyle);
            y += 20f;

            // Durée / cooldown restant
            float remaining = 0f;
            if (trackedSpell.IsActive)
                remaining = stats.spellDuration * (1f - trackedSpell.DurationProgress);
            else if (trackedSpell.IsOnCooldown)
                remaining = stats.spellCooldown * (1f - trackedSpell.CooldownProgress);

            if (remaining > 0f)
            {
                GUI.color = new Color(0.65f, 0.65f, 0.65f);
                GUI.Label(new Rect(x + 4, y, w, 14f), $"{remaining:F1}s", costStyle);
                GUI.color = Color.white;
                y += 16f;
            }
            else
            {
                y += 4f;
            }

            // Bouton activer
            bool ready = !trackedSpell.IsActive && !trackedSpell.IsOnCooldown;
            string keyHint = LocalizationManager.Get("tuto_k_attack_key");
            if (string.IsNullOrEmpty(keyHint) || keyHint == "tuto_k_attack_key") keyHint = "A";

            if (ready)
            {
                if (GUI.Button(new Rect(x + (w - 180f) * 0.5f, y, 180f, 26f), $"Activer  [{keyHint}]", btnStyle))
                    trackedSpell.TryActivate();
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.3f);
                GUI.Box(new Rect(x + (w - 180f) * 0.5f, y, 180f, 26f), $"Activer  [{keyHint}]", disabledStyle);
                GUI.color = Color.white;
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
