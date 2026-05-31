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

        [Header("Skin neutre (assigné dans l'Inspector du prefab)")]
        public Mesh neutralMesh;
        public Material neutralMaterial;

        private Camera mainCam;
        private GameObject targetHighlight;
        private static Material campHighlightMat;

        private void Start()
        {
            mainCam = Camera.main;
            maxHP    = GetMaxHP();
            currentHP = maxHP;
            UpdateCampVisual();
        }

        public int GetMaxHP()
        {
            switch (campType)
            {
                case CampType.Port:    return 200;
                case CampType.Castle:  return 800;
                default:               return 400;
            }
        }

        public void SetAsTarget(bool active)
        {
            if (active && targetHighlight == null)
            {
                if (campHighlightMat == null)
                {
                    Shader sh = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                    campHighlightMat = new Material(sh);
                    campHighlightMat.color = new Color(1f, 0.15f, 0.15f, 0.5f);
                    campHighlightMat.SetFloat("_Mode", 3);
                    campHighlightMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    campHighlightMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    campHighlightMat.SetInt("_ZWrite", 0);
                    campHighlightMat.EnableKeyword("_ALPHABLEND_ON");
                    campHighlightMat.renderQueue = 3000;
                }
                targetHighlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                targetHighlight.name = "CampTargetHighlight";
                targetHighlight.transform.position   = new Vector3(transform.position.x, 0.05f, transform.position.z);
                targetHighlight.transform.rotation   = Quaternion.identity;
                const float wr = 4f;
                targetHighlight.transform.localScale = new Vector3(wr * 2f, 0.05f, wr * 2f);

                targetHighlight.GetComponent<Renderer>().sharedMaterial = campHighlightMat;
                Destroy(targetHighlight.GetComponent<Collider>());
                targetHighlight.SetActive(false);
            }
            if (targetHighlight != null) targetHighlight.SetActive(active);
        }

        private void OnDestroy()
        {
            if (targetHighlight != null) Destroy(targetHighlight);
        }

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

        public void SetOwner(PlayerData newOwner)
        {
            if (owner == newOwner) return;

            PlayerData previousOwner = owner;
            bool wasNeutral = isNeutral;

            if (owner != null)
                owner.ownedCamps.Remove(this);

            owner = newOwner;
            isNeutral = (newOwner == null);

            if (wasNeutral && newOwner != null && HexGridGenerator.PlayerCampScale != Vector3.zero)
            {
                Vector3 oldScale     = transform.localScale;
                transform.rotation   = HexGridGenerator.PlayerCampRotation;
                transform.localScale = HexGridGenerator.PlayerCampScale;

                float ratio = oldScale.x > 0f ? oldScale.x / transform.localScale.x : 1f;
                foreach (Collider col in GetComponentsInChildren<Collider>())
                {
                    if (col is BoxCollider bc)       { bc.size *= ratio; bc.center *= ratio; }
                    else if (col is SphereCollider sc) sc.radius *= ratio;
                    else if (col is CapsuleCollider cc){ cc.radius *= ratio; cc.height *= ratio; }
                }

                if (spawnPoint != null)
                    spawnPoint.localPosition = Vector3.zero;
            }

            if (owner != null && !owner.ownedCamps.Contains(this))
                owner.ownedCamps.Add(this);

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
                if (gs != null && gs.ownerId == GameConstants.NEUTRAL_ID)
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

            if (owner == null)
            {
                MeshFilter mf = GetComponentInChildren<MeshFilter>();
                if (mf != null && neutralMesh != null) mf.sharedMesh = neutralMesh;
                if (neutralMaterial != null) rend.material = neutralMaterial;
                else rend.material.color = Color.gray;
            }
            else
            {
                rend.material.color = owner.playerColor;
            }
        }

        private void OnGUI()
        {
            if (mainCam == null) return;

            Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + Vector3.up * 3.5f);
            if (screenPos.z < 0f) return;

            const float barW = 110f;
            const float barH = 14f;
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
            { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
              normal = { textColor = Color.white } };
            GUI.Label(new Rect(x, y, barW, barH), $"{currentHP}/{maxHP}", hpStyle);

            GUI.color = prev;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
}
