using UnityEngine;

namespace SupKonQuest
{
    public class Camp : MonoBehaviour
    {
        [Header("Camp")]
        public CampType campType;
        public bool isNeutral = true;

        [Header("Owner")]
        public PlayerData owner;

        [Header("Spawn")]
        public Transform spawnPoint;

        [Header("Health")]
        public int maxHP = 300;
        public int currentHP = 300;

        private Camera mainCam;

        private void Start()
        {
            mainCam = Camera.main;
            currentHP = maxHP;
        }

        // ── Dégâts ───────────────────────────────────────────────────

        public void TakeDamage(int amount, UnitStats attacker)
        {
            currentHP = Mathf.Max(0, currentHP - amount);

            if (currentHP <= 0)
            {
                bool mutualDeath = attacker != null && attacker.currentHealth <= 0;
                PlayerData newOwner = mutualDeath ? null : (attacker != null ? GameManager.Instance?.GetPlayerById(attacker.ownerId) : null);
                SetOwner(newOwner);
                currentHP = maxHP;
            }
        }

        public void TakeDamageFromTurret(int amount)
        {
            currentHP = Mathf.Max(0, currentHP - amount);
            if (currentHP <= 0)
            {
                SetOwner(null);
                currentHP = maxHP;
            }
        }

        // ── Propriété ────────────────────────────────────────────────

        public void SetOwner(PlayerData newOwner)
        {
            if (owner == newOwner) return;

            PlayerData previousOwner = owner;

            if (owner != null)
                owner.ownedCamps.Remove(this);

            owner = newOwner;
            isNeutral = (newOwner == null);

            if (owner != null && !owner.ownedCamps.Contains(this))
                owner.ownedCamps.Add(this);

            // Détruire les gardes neutres proches quand le camp change de main
            if (newOwner != null)
                DestroyNearbyNeutralGuards();

            UpdateCampVisual();
            GameManager.Instance?.NotifyCampCaptured(this, previousOwner);
        }

        private void DestroyNearbyNeutralGuards()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 8f);
            foreach (Collider c in hits)
            {
                NeutralUnitAI guard = c.GetComponentInParent<NeutralUnitAI>();
                if (guard == null) continue;
                UnitStats gs = guard.GetComponent<UnitStats>();
                if (gs != null && gs.ownerId == 0)
                    Destroy(guard.gameObject);
            }
        }

        public void SetSpawnPosition(Vector3 worldPos)
        {
            if (spawnPoint == null)
            {
                GameObject go = new GameObject("SpawnPoint");
                go.transform.SetParent(transform);
                spawnPoint = go.transform;
            }
            spawnPoint.position = worldPos;
        }

        private void UpdateCampVisual()
        {
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend == null) return;

            if (owner != null)
            {
                RaceDefinition def = RaceRegistry.Get(owner.race);
                if (def != null)
                {
                    BuildingType btype = campType == CampType.Port   ? BuildingType.Port
                                       : campType == CampType.Castle ? BuildingType.Castle
                                       :                               BuildingType.Camp;

                    var skin = def.GetBuildingSkin(btype);
                    if (skin.HasValue)
                    {
                        MeshFilter mf = GetComponentInChildren<MeshFilter>();
                        if (mf != null && skin.Value.mesh != null)
                            mf.sharedMesh = skin.Value.mesh;

                        if (skin.Value.material != null)
                        {
                            rend.material = skin.Value.material;
                            return;
                        }
                    }
                }
            }

            rend.material.color = owner == null ? Color.gray : owner.playerColor;
        }

        // ── Barre de vie ─────────────────────────────────────────────

        private void OnGUI()
        {
            if (mainCam == null) return;

            Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            if (screenPos.z < 0f) return;

            const float barW = 60f;
            const float barH = 8f;
            float x = screenPos.x - barW * 0.5f;
            float y = Screen.height - screenPos.y - barH * 0.5f;

            // Fond sombre
            Color prev = GUI.color;
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, barW, barH), Texture2D.whiteTexture);

            // Remplissage coloré selon PV
            float ratio = maxHP > 0 ? (float)currentHP / maxHP : 1f;
            Color fill = Color.Lerp(Color.red, Color.green, ratio);
            GUI.color = new Color(fill.r, fill.g, fill.b, 0.9f);
            GUI.DrawTexture(new Rect(x, y, barW * ratio, barH), Texture2D.whiteTexture);

            // Bordure blanche fine
            GUI.color = new Color(1f, 1f, 1f, 0.4f);
            GUI.Box(new Rect(x - 1, y - 1, barW + 2, barH + 2), GUIContent.none);

            GUI.color = prev;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
}
