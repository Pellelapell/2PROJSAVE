using UnityEngine;
using UnityEngine.UI;

namespace SupKonQuest
{
    public class UnitHealthBarUI : MonoBehaviour
    {
        public Image fillImage;
        public Transform target;
        public Vector3 worldOffset = new Vector3(0, 2.5f, 0);

        private Camera mainCamera;
        private UnitStats unitStats;

        public void Initialize(UnitStats stats)
        {
            unitStats = stats;
            target = stats.transform;
            mainCamera = Camera.main;
            Refresh();
        }

        private void Update()
        {
            if (target == null || unitStats == null)
            {
                Destroy(gameObject);
                return;
            }

            if (mainCamera == null)
                mainCamera = Camera.main;

            Vector3 worldPos = target.position + worldOffset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            transform.position = screenPos;

            Refresh();
        }

        private void Refresh()
        {
            if (fillImage == null || unitStats == null) return;

            float ratio = unitStats.maxHealth > 0
                ? (float)unitStats.currentHealth / unitStats.maxHealth
                : 0f;

            fillImage.fillAmount = Mathf.Clamp01(ratio);
        }
    }
}