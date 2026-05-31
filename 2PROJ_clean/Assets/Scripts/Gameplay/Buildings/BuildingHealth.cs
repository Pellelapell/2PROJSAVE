using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    public class BuildingHealth : MonoBehaviour
    {
        public static readonly List<BuildingHealth> All = new List<BuildingHealth>();

        [Header("Health")]
        public int maxHP     = 150;
        public int currentHP = 150;
        public int ownerId   = -1;

        private Camera mainCam;

        private void Awake()  { currentHP = maxHP; }
        private void Start()  { mainCam = Camera.main; All.Add(this); }
        private void OnDestroy() { All.Remove(this); }

        public void TakeDamage(int amount)
        {
            currentHP = Mathf.Max(0, currentHP - amount);
            if (currentHP <= 0) Die();
        }

        private void Die()
        {
            Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (mainCam == null) return;

            Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + Vector3.up * 2.8f);
            if (screenPos.z < 0f) return;

            const float barW = 80f;
            const float barH = 12f;
            float x = screenPos.x - barW * 0.5f;
            float y = Screen.height - screenPos.y - barH * 0.5f;

            Color prev = GUI.color;

            GUI.color = new Color(0.05f, 0.05f, 0.05f, 0.92f);
            GUI.DrawTexture(new Rect(x - 1, y - 1, barW + 2, barH + 2), Texture2D.whiteTexture);

            float ratio = maxHP > 0 ? (float)currentHP / maxHP : 1f;
            Color fill  = Color.Lerp(new Color(0.9f, 0.1f, 0.1f), new Color(0.1f, 0.9f, 0.2f), ratio);
            GUI.color   = new Color(fill.r, fill.g, fill.b, 0.95f);
            GUI.DrawTexture(new Rect(x, y, barW * ratio, barH), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUIStyle hpStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 9, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
              normal = { textColor = Color.white } };
            GUI.Label(new Rect(x, y, barW, barH), $"{currentHP}/{maxHP}", hpStyle);

            GUI.color = prev;
        }
    }
}
