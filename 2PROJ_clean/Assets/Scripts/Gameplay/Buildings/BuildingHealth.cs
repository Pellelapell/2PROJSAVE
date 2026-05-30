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

            Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + Vector3.up * 1.8f);
            if (screenPos.z < 0f) return;

            const float barW = 50f;
            const float barH = 6f;
            float x = screenPos.x - barW * 0.5f;
            float y = Screen.height - screenPos.y - barH * 0.5f;

            Color prev = GUI.color;

            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, barW, barH), Texture2D.whiteTexture);

            float ratio = maxHP > 0 ? (float)currentHP / maxHP : 1f;
            Color fill  = Color.Lerp(Color.red, Color.green, ratio);
            GUI.color   = new Color(fill.r, fill.g, fill.b, 0.9f);
            GUI.DrawTexture(new Rect(x, y, barW * ratio, barH), Texture2D.whiteTexture);

            GUI.color = new Color(1f, 1f, 1f, 0.4f);
            GUI.Box(new Rect(x - 1, y - 1, barW + 2, barH + 2), GUIContent.none);

            GUI.color = prev;
        }
    }
}
